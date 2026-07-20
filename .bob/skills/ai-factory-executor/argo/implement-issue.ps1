#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Entry point for the Argo AI Dev factory. Implements one GitHub issue in an
    isolated Argo Workflow (own clone, own ephemeral SQL sidecar, own secret,
    network-isolated). Up to 3 run concurrently across invocations — the 4th
    queues on the semaphore.
.DESCRIPTION
    Usage (GNU-style flags, as requested):
        ./implement-issue.ps1 --issueid 6970
        ./implement-issue.ps1 --issueid 6970 --agent bob --no-watch
    PowerShell-native flags also work:
        ./implement-issue.ps1 -IssueId 6970 -AiAgent claude
.NOTES
    Replaces run-factory.ps1 for a single issue. The GitHub webhook / polling
    Sensor path is deferred (see README "Deferred").
#>
[CmdletBinding()]
param(
    [Alias('issueid')]  [int]$IssueId,
    [Alias('agent')]    [ValidateSet('claude','bob')][string]$AiAgent = 'claude',
    [Alias('repo-org')] [string]$RepoOrg  = 'ClearMeasureLabs',
    [Alias('repo-name')][string]$RepoName = 'bootcamp-palermo-workorders',
    [Alias('no-watch')] [switch]$NoWatch,
    [Alias('keep-alive')][switch]$KeepAlive
)

$ErrorActionPreference = 'Stop'

# --- Accept GNU-style "--flag value" in addition to PowerShell "-Flag value".
# PowerShell does not bind "--issueid"; translate leftover args manually.
if ($args) {
    for ($i = 0; $i -lt $args.Count; $i++) {
        switch -Regex ($args[$i]) {
            '^--issueid$'   { $IssueId  = [int]$args[++$i] }
            '^--agent$'     { $AiAgent  = $args[++$i] }
            '^--repo-org$'  { $RepoOrg  = $args[++$i] }
            '^--repo-name$' { $RepoName = $args[++$i] }
            '^--no-watch$'  { $NoWatch  = $true }
            '^--keep-alive$'{ $KeepAlive = $true }
            default { throw "Unknown argument: $($args[$i])" }
        }
    }
}
if (-not $IssueId) { throw "Missing --issueid. Usage: ./implement-issue.ps1 --issueid 6970 [--agent claude|bob] [--no-watch]" }

$env:KUBECONFIG = '/etc/rancher/k3s/k3s.yaml'
$repo = "$RepoOrg/$RepoName"

# 1. Fetch issue + build branch name (same slug rule as run-factory.ps1).
Write-Host "[->] Fetching issue #$IssueId from $repo"
$issue = gh issue view $IssueId --repo $repo --json number,title,body,url | ConvertFrom-Json
$slug  = ($issue.title.ToLower() -replace '[^a-z0-9]+','-').Trim('-')
if ($slug.Length -gt 50) { $slug = $slug.Substring(0,50).Trim('-') }
$branch = "feature/issue-$($issue.number)-$slug"

# 2. Dedup: refuse if an open PR already exists for the branch.
$pr = gh pr list --repo $repo --head $branch --state open --json number | ConvertFrom-Json
if ($pr.Count -gt 0) { Write-Host "[INFO] #$IssueId already has open PR #$($pr[0].number). Nothing to do."; return }

# 3. Per-run Secret: gh token + agent key + random SA password for this pod's DB.
$sa = (-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 24 | ForEach-Object {[char]$_})) + 'aA1!'
kubectl create secret generic "agent-secret-$IssueId" -n ai-factory `
    --from-literal=gh_token=$(gh auth token) `
    --from-literal=anthropic_api_key=$env:ANTHROPIC_API_KEY `
    --from-literal=sa-password=$sa `
    --dry-run=client -o yaml | kubectl apply -f -

# 4. Submit the isolated Workflow.
$submit = @(
    'submit','-n','ai-factory','--from','workflowtemplate/ai-dev-agent',
    '--name', "issue-$IssueId-$(Get-Random -Maximum 9999)",
    '-p', "issueNumber=$($issue.number)",
    '-p', "issueTitle=$($issue.title)",
    '-p', "issueBody=$($issue.body)",
    '-p', "issueUrl=$($issue.url)",
    '-p', "branchName=$branch",
    '-p', "repoOrg=$RepoOrg",
    '-p', "repoName=$RepoName",
    '-p', "aiAgent=$AiAgent",
    '-p', "keepAlive=$($KeepAlive.IsPresent.ToString().ToLower())"
)
if (-not $NoWatch) { $submit += '--watch' }
Write-Host "[->] Submitting Workflow for #$IssueId (agent=$AiAgent, branch=$branch)"
argo @submit
