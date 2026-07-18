#!/usr/bin/env pwsh
<#
.SYNOPSIS
    AI Factory Executor - Autonomous implementation of GitHub Projects v2 issues
.DESCRIPTION
    Discovers issues labeled "AI Factory" in "Development" column, delegates to subagents,
    monitors PRs, and orchestrates parallel execution.
.PARAMETER MaxConcurrent
    Maximum number of concurrent subagents (default: 2)
.PARAMETER Org
    GitHub organization (default: auto-detect from git remote)
.PARAMETER Repo
    GitHub repository (default: auto-detect from git remote)
.PARAMETER ProjectNumber
    GitHub Projects v2 project number (default: auto-detect)
.PARAMETER PollInterval
    Status check interval in seconds (default: 10)
.PARAMETER DryRun
    Show what would be done without executing
#>

[CmdletBinding()]
param(
    [int]$MaxConcurrent = 2,
    [string]$Org = "",
    [string]$Repo = "",
    [int]$ProjectNumber = 0,
    [int]$PollInterval = 10,
    [switch]$DryRun,
    [int]$IssueNumber = 0  # Process only this specific issue number (0 = process all)
)

$ErrorActionPreference = "Stop"

# Import container manager module
$containerManagerPath = Join-Path $PSScriptRoot "container-manager.ps1"
if (Test-Path $containerManagerPath) {
    . $containerManagerPath
    $script:UseContainers = $true
    Write-Verbose "Container manager loaded - Docker mode enabled"
} else {
    $script:UseContainers = $false
    Write-Verbose "Container manager not found - Direct mode enabled"
}

# Color output helpers
function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Progress { param([string]$Message) Write-Host "→ $Message" -ForegroundColor Cyan }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Yellow }

# Check prerequisites
function Test-Prerequisites {
    Write-Progress "Checking prerequisites..."
    
    # Check gh CLI
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Failure "GitHub CLI (gh) not found. Install: https://cli.github.com/"
        exit 1
    }
    
    # Check gh auth
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "GitHub CLI not authenticated. Run: gh auth login"
        exit 1
    }
    
    Write-Success "Prerequisites OK"
}

# Auto-detect org and repo from git remote
function Get-RepoInfo {
    if (-not $script:Org -or -not $script:Repo) {
        Write-Progress "Auto-detecting repository from git remote..."
        $remote = git remote get-url origin 2>$null
        if ($remote -match 'github\.com[:/]([^/]+)/([^/\.]+)') {
            $script:Org = $matches[1]
            $script:Repo = $matches[2]
            Write-Info "Detected: $script:Org/$script:Repo"
        } else {
            Write-Failure "Could not detect org/repo. Specify with -Org and -Repo"
            exit 1
        }
    }
}

# Get project ID from Projects v2 (OPTIONAL - not required for basic operation)
function Get-ProjectId {
    # Skip Projects v2 API - work directly with issue labels instead
    # This avoids requiring the read:project scope
    Write-Info "Working with issue labels (skipping Projects v2 API)"
    return $null
}

# Discover eligible issues
function Get-EligibleIssues {
    Write-Progress "Discovering issues with 'AI Factory' label..."
    
    # Get all open issues with "AI Factory" label
    $issuesJson = gh issue list --repo "$script:Org/$script:Repo" `
        --label "AI Factory" `
        --state open `
        --json number,title,body,url,labels `
        --limit 100
    
    $issues = $issuesJson | ConvertFrom-Json
    
    if ($issues.Count -eq 0) {
        Write-Info "No issues found with 'AI Factory' label"
        return @()
    }
    
    # Filter out issues that already have PRs
    $eligibleIssues = @()
    foreach ($issue in $issues) {
        # Check if issue has linked PRs
        $prs = gh pr list --repo "$script:Org/$script:Repo" `
            --search "closes:#$($issue.number)" `
            --json number `
            --limit 1 | ConvertFrom-Json
        
        if ($prs.Count -eq 0) {
            $eligibleIssues += $issue
        } else {
            Write-Info "Skipping issue #$($issue.number) - already has PR #$($prs[0].number)"
        }
    }
    
    Write-Success "Found $($eligibleIssues.Count) eligible issues"
    return $eligibleIssues
}

# Create subagent task for an issue
function New-SubagentTask {
    param($Issue)
    
    $titleSlug = ($Issue.title -replace '[^a-zA-Z0-9]+', '-').ToLower()
    $maxLength = [Math]::Min(50, $titleSlug.Length)
    $slug = $titleSlug.Substring(0, $maxLength)
    $branchName = "feature/issue-$($Issue.number)-$slug"
    
    $task = @"
Implement GitHub issue #$($Issue.number): $($Issue.title)

**Issue URL:** $($Issue.url)

**Description:**
$($Issue.body)

**Instructions:**
1. Create feature branch: ``$branchName``
2. Read the issue description carefully - it contains the full requirements
3. Implement the changes following the issue's acceptance criteria
4. Run tests before committing
5. Commit with message: "feat: $($Issue.title) (#$($Issue.number))"
6. Push branch and create PR with:
   - Title: $($Issue.title)
   - Body: "Closes #$($Issue.number)`n`n$($Issue.body)"
7. Monitor PR checks until all green
8. Report completion status

**Constraints:**
- Follow existing code patterns and architecture (see CLAUDE.md)
- Run trufflehog before pushing to check for secrets
- If C# changes, run stylecop
- If package.json changes, run npm audit
- If blocked or unclear, report back immediately
- Do not merge - only monitor until green
"@
    
    return @{
        Issue = $Issue
        Task = $task
        BranchName = $branchName
        StartedAt = Get-Date
    }
}

# Implement issue using Docker container or direct execution
function Invoke-IssueImplementation {
    param($SubagentTask)
    
    $issue = $SubagentTask.Issue
    $branchName = $SubagentTask.BranchName
    
    Write-Progress "Implementing Issue #$($issue.number): $($issue.title)"
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would implement issue with task:"
        Write-Host $SubagentTask.Task -ForegroundColor Gray
        return @{
            Success = $true
            Error = $null
        }
    }
    
    # Check if branch already exists (skip if already started)
    $remoteBranches = git ls-remote --heads origin $branchName 2>$null
    if ($remoteBranches) {
        Write-Info "Branch $branchName already exists remotely, skipping issue"
        return @{
            Success = $false
            Error = "Branch already exists - issue may have been started previously"
            Skipped = $true
        }
    }
    
    # Use Docker container if available, otherwise direct execution
    if ($script:UseContainers) {
        return Invoke-ContainerImplementation -SubagentTask $SubagentTask
    } else {
        return Invoke-DirectImplementation -SubagentTask $SubagentTask
    }
}

# Docker container-based implementation
function Invoke-ContainerImplementation {
    param($SubagentTask)
    
    $issue = $SubagentTask.Issue
    $branchName = $SubagentTask.BranchName
    
    try {
        Write-Progress "Starting Docker container for Issue #$($issue.number)..."
        
        # Start container
        $container = Start-AgentContainer `
            -IssueNumber $issue.number `
            -IssueTitle $issue.title `
            -IssueBody $issue.body `
            -IssueUrl $issue.url `
            -BranchName $branchName `
            -RepoOrg $script:Org `
            -RepoName $script:Repo
        
        Write-Success "Container started: $($container.ContainerName)"
        
        # Monitor until completion
        $result = Wait-ContainerCompletion -Container $container -TimeoutMinutes 30
        
        # Cleanup container
        Stop-AgentContainer -Container $container
        
        if ($result.Success) {
            Write-Success "Issue #$($issue.number) implemented successfully"
        } else {
            Write-Failure "Issue #$($issue.number) failed: $($result.Error)"
        }
        
        return $result
        
    } catch {
        Write-Failure "Container execution failed: $_"
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Direct implementation (fallback when Docker not available)
function Invoke-DirectImplementation {
    param($SubagentTask)
    
    $issue = $SubagentTask.Issue
    $branchName = $SubagentTask.BranchName
    
    try {
        # Create and checkout feature branch
        Write-Progress "Creating branch: $branchName"
        git checkout -b $branchName 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create branch $branchName"
        }
        
        # Output task for current Bob Shell session to implement
        Write-Host "`n=== TASK FOR IMPLEMENTATION ===" -ForegroundColor Cyan
        Write-Host $SubagentTask.Task -ForegroundColor White
        Write-Host "`n=== END TASK ===" -ForegroundColor Cyan
        
        # Exit and let Bob Shell handle the implementation
        Write-Host "`nPlease implement the above task in the current Bob Shell session." -ForegroundColor Yellow
        Write-Host "After implementation, commit, push, and create PR, then re-run this executor." -ForegroundColor Yellow
        
        exit 0
        
    } catch {
        Write-Failure "Failed to implement Issue #$($issue.number): $_"
        
        # Cleanup: return to master branch
        git checkout master 2>&1 | Out-Null
        
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Main orchestration loop - sequential implementation
function Start-Orchestration {
    param($Issues)
    
    $completed = [System.Collections.ArrayList]::new()
    $failed = [System.Collections.ArrayList]::new()
    $skipped = [System.Collections.ArrayList]::new()
    
    $mode = if ($script:UseContainers) { "Docker container" } else { "Direct" }
    Write-Info "Starting sequential implementation of $($Issues.Count) issues ($mode mode)"
    
    foreach ($issue in $Issues) {
        Write-Host "`n## Processing Issue #$($issue.number)" -ForegroundColor Cyan
        Write-Host "Title: $($issue.title)" -ForegroundColor Yellow
        
        $task = New-SubagentTask -Issue $issue
        $result = Invoke-IssueImplementation -SubagentTask $task
        
        if ($result.Skipped) {
            $skipped.Add($issue) | Out-Null
            Write-Info "Issue #$($issue.number) skipped (already started)"
        } elseif ($result.Success) {
            $completed.Add($issue) | Out-Null
            Write-Success "Issue #$($issue.number) complete"
        } else {
            $failed.Add(@{
                Issue = $issue
                Error = $result.Error
            }) | Out-Null
            Write-Failure "Issue #$($issue.number) failed: $($result.Error)"
        }
        
        # Return to master branch before next issue (only in direct mode)
        if (-not $script:UseContainers) {
            git checkout master 2>&1 | Out-Null
        }
    }
    
    # Final report
    Write-Host "`n## AI Factory Execution Complete" -ForegroundColor Green
    Write-Host "`nSummary:"
    Write-Host "  Total Issues: $($Issues.Count)"
    Write-Host "  Completed: $($completed.Count)"
    Write-Host "  Skipped: $($skipped.Count)"
    Write-Host "  Failed: $($failed.Count)"
    
    if ($completed.Count -gt 0) {
        Write-Host "`nCompleted Issues:" -ForegroundColor Green
        foreach ($issue in $completed) {
            Write-Host "  ✓ Issue #$($issue.number): $($issue.title)"
        }
    }
    
    if ($skipped.Count -gt 0) {
        Write-Host "`nSkipped Issues:" -ForegroundColor Yellow
        foreach ($issue in $skipped) {
            Write-Host "  ⊘ Issue #$($issue.number): $($issue.title)"
        }
    }
    
    if ($failed.Count -gt 0) {
        Write-Host "`nFailed Issues:" -ForegroundColor Red
        foreach ($item in $failed) {
            Write-Host "  ✗ Issue #$($item.Issue.number): $($item.Issue.title)"
            Write-Host "    Error: $($item.Error)" -ForegroundColor Gray
        }
    }
}

# Main execution
try {
    Write-Host "=== AI Factory Executor ===" -ForegroundColor Cyan
    Write-Host ""
    
    Test-Prerequisites
    Get-RepoInfo
    
    # Skip Projects v2 API - work directly with issue labels
    Get-ProjectId | Out-Null
    $issues = Get-EligibleIssues
    
    # Filter to specific issue if requested
    if ($IssueNumber -gt 0) {
        $issues = $issues | Where-Object { $_.number -eq $IssueNumber }
        if ($issues.Count -eq 0) {
            Write-Failure "Issue #$IssueNumber not found or not eligible (must have 'AI Factory' label)"
            exit 1
        }
        Write-Info "Processing only Issue #$IssueNumber"
    }
    
    if ($issues.Count -eq 0) {
        Write-Info "No eligible issues to process"
        exit 0
    }
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would process $($issues.Count) issues:"
        foreach ($issue in $issues) {
            Write-Host "  - Issue #$($issue.number): $($issue.title)"
        }
        exit 0
    }
    
    Start-Orchestration -Issues $issues
    
} catch {
    Write-Failure "Execution failed: $_"
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
