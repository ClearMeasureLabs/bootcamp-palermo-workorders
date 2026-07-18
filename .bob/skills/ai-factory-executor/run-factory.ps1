#!/usr/bin/env pwsh
<#
.SYNOPSIS
    AI Factory orchestrator. Discovers open issues labeled "AI Factory" and
    implements them one at a time, each inside its own agent container.
.DESCRIPTION
    Sequential (one-at-a-time) execution: for each eligible issue a container is
    started that clones the repo, runs the selected AI agent to implement the
    issue, commits, pushes, and opens a PR. Issues that already have an open PR
    are skipped so the orchestrator is safe to re-run.
.EXAMPLE
    ./run-factory.ps1 -AiAgent claude -MonitorChecks $false
#>
[CmdletBinding()]
param(
    [ValidateSet("claude", "bob")]
    [string]$AiAgent = "claude",

    [string]$Label = "AI Factory",

    # Optional whitelist of issue numbers to process. When provided, only these
    # issues are implemented (still must carry the label). Useful when the label
    # is shared by many unrelated issues.
    [int[]]$IssueNumbers = @(),

    [string]$RepoOrg = "ClearMeasureLabs",

    [string]$RepoName = "bootcamp-palermo-workorders",

    # When false, each container creates its PR and exits without waiting for CI
    # (checks still run async on GitHub; verify collectively afterward).
    [bool]$MonitorChecks = $false,

    [bool]$RunQualityGates = $true,

    [bool]$EnableDocker = $false,

    [int]$TimeoutMinutes = 60
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "container-manager.ps1")

$repo = "$RepoOrg/$RepoName"

Write-Host ""
Write-Host "=== AI Factory Orchestrator ===" -ForegroundColor Cyan
Write-Info "Repo:  $repo"
Write-Info "Label: $Label"
Write-Info "Agent: $AiAgent"
Write-Info "MonitorChecks: $MonitorChecks"
Write-Info "RunQualityGates: $RunQualityGates"
Write-Host ""

# 1. Discover eligible issues (labeled, open, no existing open PR)
Write-Progress "Discovering issues labeled '$Label'..."
$issues = gh issue list --repo $repo --label $Label --state open `
    --json number,title,body,url --limit 100 | ConvertFrom-Json | Sort-Object number

if (-not $issues -or $issues.Count -eq 0) {
    Write-Info "No open issues found with label '$Label'. Nothing to do."
    return
}

Write-Success "Found $($issues.Count) labeled issue(s)"

if ($IssueNumbers.Count -gt 0) {
    $issues = @($issues | Where-Object { $IssueNumbers -contains $_.number })
    Write-Info "Filtered to $($issues.Count) issue(s) by IssueNumbers whitelist"
}

$queue = @()
foreach ($issue in $issues) {
    $slug = ($issue.title.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
    if ($slug.Length -gt 50) { $slug = $slug.Substring(0, 50).Trim('-') }
    $branch = "feature/issue-$($issue.number)-$slug"

    $existingPr = gh pr list --repo $repo --head $branch --state open --json number | ConvertFrom-Json
    if ($existingPr.Count -gt 0) {
        Write-Info "Skipping #$($issue.number) (already has open PR #$($existingPr[0].number))"
        continue
    }
    $queue += [PSCustomObject]@{ Issue = $issue; Branch = $branch }
}

if ($queue.Count -eq 0) {
    Write-Info "All labeled issues already have open PRs. Nothing to do."
    return
}

Write-Success "$($queue.Count) issue(s) queued for implementation"
Write-Host ""

# 2. Process one at a time
$results = @()
$index = 0
foreach ($item in $queue) {
    $index++
    $issue = $item.Issue
    Write-Host ("-" * 70) -ForegroundColor DarkGray
    Write-Progress "[$index/$($queue.Count)] Issue #$($issue.number): $($issue.title)"
    Write-Host ("-" * 70) -ForegroundColor DarkGray

    $container = $null
    $result = $null
    try {
        $container = Start-AgentContainer `
            -IssueNumber $issue.number `
            -IssueTitle $issue.title `
            -IssueBody $issue.body `
            -IssueUrl $issue.url `
            -BranchName $item.Branch `
            -RepoOrg $RepoOrg `
            -RepoName $RepoName `
            -AiAgent $AiAgent `
            -MonitorChecks $MonitorChecks `
            -RunQualityGates $RunQualityGates `
            -EnableDocker $EnableDocker

        $result = Wait-ContainerCompletion -Container $container -TimeoutMinutes $TimeoutMinutes
    } catch {
        $result = @{ Success = $false; Error = $_.Exception.Message }
    } finally {
        if ($container) { Stop-AgentContainer -Container $container -Force }
    }

    # Resolve the PR number created for this issue's branch
    $prNumber = $null
    try {
        $pr = gh pr list --repo $repo --head $item.Branch --state open --json number | ConvertFrom-Json
        if ($pr.Count -gt 0) { $prNumber = $pr[0].number }
    } catch { }

    $results += [PSCustomObject]@{
        Issue   = $issue.number
        Title   = $issue.title
        Success = [bool]$result.Success
        PR      = $prNumber
        Error   = $result.Error
    }

    if ($result.Success) {
        Write-Success "Issue #$($issue.number) done (PR #$prNumber)"
    } else {
        Write-Failure "Issue #$($issue.number) failed: $($result.Error)"
    }
    Write-Host ""
}

# 3. Summary
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "AI Factory Summary" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
$results | Format-Table -AutoSize Issue, Success, PR, Title
$ok = @($results | Where-Object { $_.Success }).Count
Write-Host ""
Write-Success "$ok / $($results.Count) issues implemented"
$failed = @($results | Where-Object { -not $_.Success })
if ($failed.Count -gt 0) {
    Write-Failure "$($failed.Count) failed:"
    foreach ($f in $failed) { Write-Host "  - #$($f.Issue): $($f.Error)" -ForegroundColor Red }
}

return $results
