#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stand up the Argo-based AI Dev factory on a single-node k3s cluster.
.DESCRIPTION
    Idempotent. Installs k3s (with a NetworkPolicy-capable CNI), Argo Workflows,
    Argo Events, imports the ai-factory-agent image, and applies all manifests.
    Pin versions before running in anything real. New infra — get approval first.
#>
[CmdletBinding()]
param(
    [string]$ArgoWorkflowsVersion = "v3.5.11",
    [string]$ArgoEventsVersion    = "v1.9.3",
    [int]$Parallelism = 3
)
$ErrorActionPreference = "Stop"
$here = $PSScriptRoot

# 1. k3s. Disable flannel's built-in and use kube-router so NetworkPolicy is enforced.
if (-not (Get-Command k3s -ErrorAction SilentlyContinue)) {
    Write-Host "Installing k3s (NetworkPolicy enforced)..."
    curl -sfL https://get.k3s.io | sh -s - --disable traefik
}
$env:KUBECONFIG = "/etc/rancher/k3s/k3s.yaml"

# 2. Namespace
kubectl create namespace ai-factory --dry-run=client -o yaml | kubectl apply -f -

# 3. Argo Workflows + Events (pinned)
kubectl apply -n ai-factory -f "https://github.com/argoproj/argo-workflows/releases/download/$ArgoWorkflowsVersion/install.yaml"
kubectl apply -n ai-factory -f "https://raw.githubusercontent.com/argoproj/argo-events/$ArgoEventsVersion/manifests/install.yaml"
kubectl apply -n ai-factory -f "https://raw.githubusercontent.com/argoproj/argo-events/$ArgoEventsVersion/manifests/install-validating-webhook.yaml"

# 4. Controller parallelism cap = 3 concurrent Workflows.
kubectl patch configmap workflow-controller-configmap -n ai-factory --type merge `
    -p "{`"data`":{`"parallelism`":`"$Parallelism`"}}"

# 5. Build + import the agent image into k3s containerd (no registry needed).
docker build -t ai-factory-agent:latest -f "$here/../Dockerfile.agent" "$here/.."
docker save ai-factory-agent:latest | sudo k3s ctr images import -

# 6. Cluster-wide credentials the dispatcher uses to poll GitHub.
$token = gh auth token
kubectl create secret generic ai-factory-gh -n ai-factory `
    --from-literal=gh_token=$token --dry-run=client -o yaml | kubectl apply -f -

# 7. Apply the core manifests. Entry point is the CLI (implement-issue.ps1);
# the automated trigger (dispatcher/eventsource/sensor) is DEFERRED — apply
# those separately if/when polling or a webhook is wanted.
kubectl apply -f "$here/semaphore-config.yaml"
kubectl apply -f "$here/rbac.yaml"
kubectl apply -f "$here/networkpolicy.yaml"
kubectl apply -f "$here/workflowtemplate-agent.yaml"

# Deferred trigger automation (uncomment to enable polling):
# kubectl create configmap ai-factory-dispatch-script -n ai-factory `
#     --from-file=dispatch.ps1="$here/scripts/dispatch.ps1" --dry-run=client -o yaml | kubectl apply -f -
# kubectl apply -f "$here/workflowtemplate-dispatcher.yaml"
# kubectl apply -f "$here/eventsource.yaml"
# kubectl apply -f "$here/sensor.yaml"

Write-Host "Done. Implement an issue: ./implement-issue.ps1 --issueid 6970"
Write-Host "Watch runs: argo list -n ai-factory  |  UI: argo server then :2746"
