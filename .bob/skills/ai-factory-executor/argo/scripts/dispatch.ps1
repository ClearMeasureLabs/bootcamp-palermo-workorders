#!/usr/bin/env pwsh
# Discovery + dedup + submit — ported from run-factory.ps1 (steps 1-2), but
# submits an Argo Workflow per issue instead of running a container inline.
$ErrorActionPreference = "Stop"

$repo   = "$env:REPO_ORG/$env:REPO_NAME"
$label  = $env:LABEL
$agent  = $env:AI_AGENT
$env:GH_TOKEN = (Get-Content /run/secrets/gh_token -Raw).Trim()

Write-Host "Discovering issues labeled '$label' in $repo..."
$issues = gh issue list --repo $repo --label $label --state open `
    --json number,title,body,url --limit 100 | ConvertFrom-Json | Sort-Object number
if (-not $issues) { Write-Host "No eligible issues."; exit 0 }

foreach ($issue in $issues) {
    $slug = ($issue.title.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
    if ($slug.Length -gt 50) { $slug = $slug.Substring(0, 50).Trim('-') }
    $branch = "feature/issue-$($issue.number)-$slug"

    # Dedup: skip if an open PR already exists for the branch.
    $pr = gh pr list --repo $repo --head $branch --state open --json number | ConvertFrom-Json
    if ($pr.Count -gt 0) { Write-Host "skip #$($issue.number) (PR #$($pr[0].number))"; continue }

    # Skip if a Workflow for this issue is already active.
    $active = argo list -n ai-factory --running -o name 2>$null | Select-String "issue-$($issue.number)-"
    if ($active) { Write-Host "skip #$($issue.number) (already running)"; continue }

    # Per-run Secret: gh token + random SA password for this pod's SQL sidecar.
    $sa = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 24 | ForEach-Object {[char]$_}) + "aA1!"
    kubectl create secret generic "agent-secret-$($issue.number)" -n ai-factory `
        --from-literal=gh_token=$env:GH_TOKEN `
        --from-literal=anthropic_api_key=$env:ANTHROPIC_API_KEY `
        --from-literal=sa-password=$sa `
        --dry-run=client -o yaml | kubectl apply -f -

    Write-Host "submit #$($issue.number): $($issue.title)"
    argo submit -n ai-factory --from workflowtemplate/ai-dev-agent `
        --name "issue-$($issue.number)-$(Get-Random -Maximum 9999)" `
        -p issueNumber="$($issue.number)" `
        -p issueTitle="$($issue.title)" `
        -p issueBody="$($issue.body)" `
        -p issueUrl="$($issue.url)" `
        -p branchName="$branch" `
        -p repoOrg="$env:REPO_ORG" `
        -p repoName="$env:REPO_NAME" `
        -p aiAgent="$agent"
}
