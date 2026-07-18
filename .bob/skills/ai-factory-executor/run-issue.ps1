#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Launches an AI Factory agent container for a single GitHub issue and streams
    its logs until completion.
.EXAMPLE
    ./run-issue.ps1 -IssueNumber 6970 -AiAgent claude -BranchName feature/issue-6970-claude-test
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [int]$IssueNumber,

    [ValidateSet("claude", "bob")]
    [string]$AiAgent = "claude",

    [string]$BranchName,

    [string]$RepoOrg = "ClearMeasureLabs",

    [string]$RepoName = "bootcamp-palermo-workorders",

    [bool]$MonitorChecks = $true,

    [bool]$RunQualityGates = $true,

    [int]$TimeoutMinutes = 60
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "container-manager.ps1")

# Fetch issue details from GitHub
Write-Progress "Fetching issue #$IssueNumber..."
$issueJson = gh issue view $IssueNumber --repo "$RepoOrg/$RepoName" --json number,title,body,url | ConvertFrom-Json

$issueTitle = $issueJson.title
$issueBody = $issueJson.body
$issueUrl = $issueJson.url

if (-not $BranchName) {
    $slug = ($issueTitle.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
    if ($slug.Length -gt 50) { $slug = $slug.Substring(0, 50).Trim('-') }
    $BranchName = "feature/issue-$IssueNumber-$slug"
}

Write-Info "Issue:  #$IssueNumber - $issueTitle"
Write-Info "Agent:  $AiAgent"
Write-Info "Branch: $BranchName"

$container = Start-AgentContainer `
    -IssueNumber $IssueNumber `
    -IssueTitle $issueTitle `
    -IssueBody $issueBody `
    -IssueUrl $issueUrl `
    -BranchName $BranchName `
    -RepoOrg $RepoOrg `
    -RepoName $RepoName `
    -AiAgent $AiAgent `
    -MonitorChecks $MonitorChecks `
    -RunQualityGates $RunQualityGates

try {
    $result = Wait-ContainerCompletion -Container $container -TimeoutMinutes $TimeoutMinutes
    if ($result.Success) {
        Write-Success "Issue #$IssueNumber completed successfully"
    } else {
        Write-Failure "Issue #$IssueNumber failed: $($result.Error)"
    }
    return $result
} finally {
    Stop-AgentContainer -Container $container -Force
}
