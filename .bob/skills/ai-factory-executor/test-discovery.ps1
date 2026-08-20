#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test script for AI Factory issue discovery
.DESCRIPTION
    Validates GitHub API access and issue discovery without executing any implementations.
    Use this to verify the skill can find eligible issues before running the full executor.
#>

[CmdletBinding()]
param(
    [string]$Org = "",
    [string]$Repo = ""
)

$ErrorActionPreference = "Stop"

function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Yellow }

Write-Host "=== AI Factory Discovery Test ===" -ForegroundColor Cyan
Write-Host ""

# Check gh CLI
Write-Host "Checking GitHub CLI..." -NoNewline
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Failure "GitHub CLI (gh) not found"
    Write-Host "Install from: https://cli.github.com/"
    exit 1
}
Write-Success " OK"

# Check auth
Write-Host "Checking GitHub authentication..." -NoNewline
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Failure "Not authenticated"
    Write-Host "Run: gh auth login"
    exit 1
}
Write-Success " OK"

# Auto-detect repo
if (-not $Org -or -not $Repo) {
    Write-Host "Auto-detecting repository..." -NoNewline
    $remote = git remote get-url origin 2>$null
    if ($remote -match 'github\.com[:/]([^/]+)/([^/\.]+)') {
        $Org = $matches[1]
        $Repo = $matches[2]
        Write-Success " $Org/$Repo"
    } else {
        Write-Host ""
        Write-Failure "Could not detect org/repo from git remote"
        Write-Host "Specify with: -Org <org> -Repo <repo>"
        exit 1
    }
}

# Query issues
Write-Host "Querying issues with 'AI Factory' label..." -NoNewline
try {
    $issuesJson = gh issue list --repo "$Org/$Repo" `
        --label "AI Factory" `
        --state open `
        --json number,title,body,url,labels,state `
        --limit 100
    
    $issues = $issuesJson | ConvertFrom-Json
    Write-Success " Found $($issues.Count) issues"
} catch {
    Write-Host ""
    Write-Failure "Failed to query issues: $_"
    exit 1
}

if ($issues.Count -eq 0) {
    Write-Info "No issues found with 'AI Factory' label"
    Write-Host ""
    Write-Host "To test this skill, create an issue with:"
    Write-Host "  1. Label: 'AI Factory'"
    Write-Host "  2. Add to a GitHub Project"
    Write-Host "  3. Move to 'Development' column"
    exit 0
}

# Display issues
Write-Host ""
Write-Host "Issues with 'AI Factory' label:" -ForegroundColor Cyan
Write-Host ""

foreach ($issue in $issues) {
    # Check for existing PRs
    $prs = gh pr list --repo "$Org/$Repo" `
        --search "closes:#$($issue.number)" `
        --json number,state `
        --limit 1 | ConvertFrom-Json
    
    $hasPR = $prs.Count -gt 0
    $status = if ($hasPR) { "HAS PR #$($prs[0].number)" } else { "READY" }
    $color = if ($hasPR) { "Gray" } else { "Green" }
    
    Write-Host "  Issue #$($issue.number): " -NoNewline
    Write-Host $issue.title -ForegroundColor White
    Write-Host "    Status: " -NoNewline
    Write-Host $status -ForegroundColor $color
    Write-Host "    URL: $($issue.url)" -ForegroundColor Gray
    
    if ($issue.body) {
        $preview = $issue.body.Substring(0, [Math]::Min(100, $issue.body.Length))
        if ($issue.body.Length -gt 100) { $preview += "..." }
        Write-Host "    Preview: $preview" -ForegroundColor Gray
    }
    Write-Host ""
}

# Summary
$readyCount = ($issues | Where-Object { 
    $issueNum = $_.number
    $prs = gh pr list --repo "$Org/$Repo" `
        --search "closes:#$issueNum" `
        --json number `
        --limit 1 | ConvertFrom-Json
    $prs.Count -eq 0
}).Count

Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total issues with 'AI Factory' label: $($issues.Count)"
Write-Host "  Ready to implement (no PR): $readyCount"
Write-Host "  Already have PRs: $($issues.Count - $readyCount)"
Write-Host ""

if ($readyCount -gt 0) {
    Write-Success "Ready to run AI Factory Executor on $readyCount issue(s)"
    Write-Host ""
    Write-Host "Run: pwsh .bob/skills/ai-factory-executor/executor.ps1"
} else {
    Write-Info "All issues already have PRs - nothing to implement"
}
