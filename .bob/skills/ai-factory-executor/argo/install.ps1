#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stand up the Argo-based AI Dev factory on a Kubernetes cluster.
.DESCRIPTION
    Idempotent. Auto-detects the cluster provider:
      * k3s          - single-node k3s (installs it if absent; imports the image
                       into k3s containerd via `k3s ctr images import`).
      * docker-desktop / generic - uses the CURRENT kubectl context. On Docker
                       Desktop the containerd image store is shared with the
                       daemon, so a locally-built image is visible to pods with
                       imagePullPolicy: Never (no registry / no import needed).

    Installs Argo Workflows (cluster install, controller in the `argo`
    namespace, manages workflows in all namespaces), caps controller
    parallelism to 3, builds/loads the agent image, creates the cluster GitHub
    secret, and applies the core manifests into the `ai-factory` namespace.

    Argo Events (the deferred polling/webhook trigger) is only installed with
    -WithEvents. The default entry point is the CLI (implement-issue.ps1).

    Pin versions before running in anything real. New infra -> get approval first.
.PARAMETER Provider
    auto (default) | k3s | docker-desktop. 'auto' picks k3s if the k3s kubeconfig
    exists, otherwise the current kubectl context.
#>
[CmdletBinding()]
param(
    [ValidateSet('auto','k3s','docker-desktop')][string]$Provider = 'auto',
    [string]$ArgoWorkflowsVersion = "v3.5.11",
    [string]$ArgoEventsVersion    = "v1.9.3",
    [int]$Parallelism = 7,
    [switch]$WithEvents,
    [switch]$SkipImageBuild
)
$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$k3sKubeconfig = "/etc/rancher/k3s/k3s.yaml"

# 0. Resolve the provider.
if ($Provider -eq 'auto') {
    $Provider = if (Test-Path $k3sKubeconfig) { 'k3s' } else { 'docker-desktop' }
}
Write-Host "[install] provider = $Provider"

# 1. Cluster bring-up (k3s only). Docker Desktop / generic clusters are assumed
#    to already exist and be selected as the current kubectl context.
if ($Provider -eq 'k3s') {
    if (-not (Get-Command k3s -ErrorAction SilentlyContinue)) {
        Write-Host "Installing k3s (NetworkPolicy enforced)..."
        curl -sfL https://get.k3s.io | sh -s - --disable traefik
    }
    $env:KUBECONFIG = $k3sKubeconfig
} else {
    # Use whatever context the caller has selected (e.g. docker-desktop).
    $ctx = (kubectl config current-context) 2>$null
    Write-Host "[install] using current kubectl context: $ctx"
}

# 2. Namespaces.
kubectl create namespace ai-factory --dry-run=client -o yaml | kubectl apply -f -
kubectl create namespace argo        --dry-run=client -o yaml | kubectl apply -f -

# 3. Argo Workflows (cluster install: controller in `argo` ns, manages all namespaces).
kubectl apply -n argo -f "https://github.com/argoproj/argo-workflows/releases/download/$ArgoWorkflowsVersion/install.yaml"

# 3b. Argo Events (deferred trigger only) - opt-in.
if ($WithEvents) {
    kubectl create namespace argo-events --dry-run=client -o yaml | kubectl apply -f -
    kubectl apply -n argo-events -f "https://raw.githubusercontent.com/argoproj/argo-events/$ArgoEventsVersion/manifests/install.yaml"
    kubectl apply -n argo-events -f "https://raw.githubusercontent.com/argoproj/argo-events/$ArgoEventsVersion/manifests/install-validating-webhook.yaml"
}

# 4. Controller parallelism cap = 3 concurrent Workflows. The controller reads
#    its ConfigMap from its own namespace (argo).
kubectl -n argo rollout status deploy/workflow-controller --timeout=120s 2>$null | Out-Null
kubectl patch configmap workflow-controller-configmap -n argo --type merge `
    -p "{`"data`":{`"parallelism`":`"$Parallelism`"}}"
# Restart the controller so it picks up the new parallelism immediately.
kubectl -n argo rollout restart deploy/workflow-controller | Out-Null

# 5. Build + load the agent image.
if (-not $SkipImageBuild) {
    # Prefer the claude-only Dockerfile when the private bobshell tarball is
    # absent from the build context (the default in this environment).
    $dockerfile = if (Test-Path "$here/../bobshell-1.0.6.tgz") { "$here/../Dockerfile.agent" }
                  else { "$here/../Dockerfile.agent.claude" }
    Write-Host "[install] building ai-factory-agent:latest from $dockerfile"
    docker build -t ai-factory-agent:latest -f $dockerfile "$here/.."
    if ($Provider -eq 'k3s') {
        # k3s runs its own containerd; import the image into it.
        docker save ai-factory-agent:latest | sudo k3s ctr images import -
    }
    # Docker Desktop shares its containerd image store with the daemon, so the
    # just-built image is already visible to pods (imagePullPolicy: Never).
}

# 6. Cluster-wide GitHub credentials (used by the deferred dispatcher; harmless otherwise).
$token = gh auth token
kubectl create secret generic ai-factory-gh -n ai-factory `
    --from-literal=gh_token=$token --dry-run=client -o yaml | kubectl apply -f -

# 7. Apply the core manifests. Entry point is the CLI (implement-issue.ps1);
#    the automated trigger (dispatcher/eventsource/sensor) stays deferred.
kubectl apply -f "$here/semaphore-config.yaml"
kubectl apply -f "$here/rbac.yaml"
kubectl apply -f "$here/networkpolicy.yaml"
kubectl apply -f "$here/workflowtemplate-agent.yaml"

if ($WithEvents) {
    kubectl create configmap ai-factory-dispatch-script -n ai-factory `
        --from-file=dispatch.ps1="$here/scripts/dispatch.ps1" --dry-run=client -o yaml | kubectl apply -f -
    kubectl apply -f "$here/workflowtemplate-dispatcher.yaml"
    kubectl apply -f "$here/eventsource.yaml"
    kubectl apply -f "$here/sensor.yaml"
}

Write-Host "Done. Implement an issue: ./implement-issue.ps1 --issueid 6970"
Write-Host "Watch runs: argo list -n ai-factory   |   UI: argo server then :2746"
