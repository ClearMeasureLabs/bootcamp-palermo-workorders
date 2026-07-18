#!/usr/bin/env pwsh
<#
.SYNOPSIS
    PR monitoring and status checking for AI Factory
.DESCRIPTION
    Monitors GitHub pull requests until all checks pass, with timeout and retry logic.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Color output helpers
function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Progress { param([string]$Message) Write-Host "→ $Message" -ForegroundColor Cyan }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Yellow }

<#
.SYNOPSIS
    Gets the current status of PR checks
#>
function Get-PrCheckStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoOrg,
        
        [Parameter(Mandatory)]
        [string]$RepoName,
        
        [Parameter(Mandatory)]
        [int]$PrNumber
    )
    
    try {
        $checksJson = gh pr checks $PrNumber --repo "$RepoOrg/$RepoName" --json state,name,conclusion 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            return @{
                Success = $false
                Error = "Failed to query PR checks: $checksJson"
                Checks = @()
            }
        }
        
        $checks = $checksJson | ConvertFrom-Json
        
        if (-not $checks) {
            return @{
                Success = $true
                Checks = @()
                AllPassed = $false
                HasChecks = $false
            }
        }
        
        # Categorize checks
        $pending = @($checks | Where-Object { $_.state -in @("PENDING", "QUEUED", "IN_PROGRESS", "WAITING") })
        $failed = @($checks | Where-Object { 
            $_.state -in @("FAILURE", "CANCELLED", "TIMED_OUT", "ACTION_REQUIRED") -or
            $_.conclusion -in @("FAILURE", "CANCELLED", "TIMED_OUT", "ACTION_REQUIRED")
        })
        $success = @($checks | Where-Object { 
            $_.state -eq "SUCCESS" -or $_.conclusion -eq "SUCCESS"
        })
        $skipped = @($checks | Where-Object { 
            $_.state -eq "SKIPPED" -or $_.conclusion -eq "SKIPPED" -or
            $_.state -eq "NEUTRAL" -or $_.conclusion -eq "NEUTRAL"
        })
        
        $allPassed = ($pending.Count -eq 0 -and $failed.Count -eq 0 -and $success.Count -gt 0)
        
        return @{
            Success = $true
            Checks = $checks
            HasChecks = ($checks.Count -gt 0)
            AllPassed = $allPassed
            Pending = $pending
            Failed = $failed
            Success = $success
            Skipped = $skipped
            Summary = @{
                Total = $checks.Count
                Pending = $pending.Count
                Failed = $failed.Count
                Success = $success.Count
                Skipped = $skipped.Count
            }
        }
        
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
            Checks = @()
        }
    }
}

<#
.SYNOPSIS
    Gets PR review status
#>
function Get-PrReviewStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoOrg,
        
        [Parameter(Mandatory)]
        [string]$RepoName,
        
        [Parameter(Mandatory)]
        [int]$PrNumber
    )
    
    try {
        $reviewsJson = gh pr view $PrNumber --repo "$RepoOrg/$RepoName" --json reviews 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            return @{
                Success = $false
                Error = "Failed to query PR reviews: $reviewsJson"
            }
        }
        
        $data = $reviewsJson | ConvertFrom-Json
        $reviews = $data.reviews
        
        if (-not $reviews) {
            return @{
                Success = $true
                HasReviews = $false
                Approved = $false
                ChangesRequested = $false
                Reviews = @()
            }
        }
        
        # Get latest review state per reviewer
        $latestReviews = @{}
        foreach ($review in $reviews) {
            $author = $review.author.login
            if (-not $latestReviews.ContainsKey($author) -or 
                $review.submittedAt -gt $latestReviews[$author].submittedAt) {
                $latestReviews[$author] = $review
            }
        }
        
        $approved = @($latestReviews.Values | Where-Object { $_.state -eq "APPROVED" })
        $changesRequested = @($latestReviews.Values | Where-Object { $_.state -eq "CHANGES_REQUESTED" })
        
        return @{
            Success = $true
            HasReviews = ($reviews.Count -gt 0)
            Approved = ($approved.Count -gt 0)
            ChangesRequested = ($changesRequested.Count -gt 0)
            Reviews = $latestReviews.Values
            ApprovedCount = $approved.Count
            ChangesRequestedCount = $changesRequested.Count
        }
        
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

<#
.SYNOPSIS
    Waits for PR checks to pass
#>
function Wait-PrChecks {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoOrg,
        
        [Parameter(Mandatory)]
        [string]$RepoName,
        
        [Parameter(Mandatory)]
        [int]$PrNumber,
        
        [int]$TimeoutMinutes = 30,
        
        [int]$PollIntervalSeconds = 30,
        
        [switch]$RequireReviews,
        
        [switch]$Verbose
    )
    
    $startTime = Get-Date
    $iteration = 0
    
    Write-Progress "Monitoring PR #$PrNumber checks..."
    
    while ($true) {
        $iteration++
        
        # Check timeout
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalMinutes -gt $TimeoutMinutes) {
            Write-Failure "Timeout waiting for PR checks (${TimeoutMinutes}m)"
            return @{
                Success = $false
                TimedOut = $true
                Error = "Timeout after $TimeoutMinutes minutes"
                ElapsedMinutes = [int]$elapsed.TotalMinutes
            }
        }
        
        # Get check status
        $checkStatus = Get-PrCheckStatus -RepoOrg $RepoOrg -RepoName $RepoName -PrNumber $PrNumber
        
        if (-not $checkStatus.Success) {
            Write-Warning "Failed to query checks: $($checkStatus.Error)"
            Start-Sleep -Seconds $PollIntervalSeconds
            continue
        }
        
        # Display status
        if ($checkStatus.HasChecks) {
            $summary = $checkStatus.Summary
            Write-Info "PR checks: $($summary.Success) passed, $($summary.Pending) pending, $($summary.Failed) failed, $($summary.Skipped) skipped"
            
            if ($Verbose -and $checkStatus.Pending.Count -gt 0) {
                Write-Host "  Pending checks:" -ForegroundColor Gray
                foreach ($check in $checkStatus.Pending) {
                    Write-Host "    - $($check.name)" -ForegroundColor Gray
                }
            }
        } else {
            Write-Info "No checks found yet (iteration $iteration)..."
        }
        
        # Check for failures
        if ($checkStatus.Failed.Count -gt 0) {
            Write-Failure "PR checks failed:"
            foreach ($check in $checkStatus.Failed) {
                Write-Host "  - $($check.name): $($check.state) / $($check.conclusion)" -ForegroundColor Red
            }
            
            return @{
                Success = $false
                Failed = $true
                Error = "PR checks failed"
                FailedChecks = $checkStatus.Failed
                AllChecks = $checkStatus.Checks
            }
        }
        
        # Check if all passed
        if ($checkStatus.AllPassed) {
            # If reviews required, check review status
            if ($RequireReviews) {
                $reviewStatus = Get-PrReviewStatus -RepoOrg $RepoOrg -RepoName $RepoName -PrNumber $PrNumber
                
                if ($reviewStatus.Success) {
                    if ($reviewStatus.ChangesRequested) {
                        Write-Failure "PR has changes requested"
                        return @{
                            Success = $false
                            Error = "Changes requested in reviews"
                            ReviewStatus = $reviewStatus
                        }
                    }
                    
                    if (-not $reviewStatus.Approved) {
                        Write-Info "Waiting for PR approval..."
                        Start-Sleep -Seconds $PollIntervalSeconds
                        continue
                    }
                    
                    Write-Success "PR approved and all checks passed!"
                } else {
                    Write-Warning "Failed to query reviews: $($reviewStatus.Error)"
                }
            }
            
            Write-Success "All PR checks passed!"
            return @{
                Success = $true
                AllPassed = $true
                CheckCount = $checkStatus.Summary.Success
                ElapsedMinutes = [int]$elapsed.TotalMinutes
                Iterations = $iteration
            }
        }
        
        # Wait before next check
        Start-Sleep -Seconds $PollIntervalSeconds
    }
}

<#
.SYNOPSIS
    Gets detailed PR information
#>
function Get-PrInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoOrg,
        
        [Parameter(Mandatory)]
        [string]$RepoName,
        
        [Parameter(Mandatory)]
        [int]$PrNumber
    )
    
    try {
        $prJson = gh pr view $PrNumber --repo "$RepoOrg/$RepoName" --json number,title,state,url,headRefName,baseRefName,mergeable,mergeStateStatus 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            return @{
                Success = $false
                Error = "Failed to query PR: $prJson"
            }
        }
        
        $pr = $prJson | ConvertFrom-Json
        
        return @{
            Success = $true
            Number = $pr.number
            Title = $pr.title
            State = $pr.state
            Url = $pr.url
            HeadBranch = $pr.headRefName
            BaseBranch = $pr.baseRefName
            Mergeable = $pr.mergeable
            MergeState = $pr.mergeStateStatus
        }
        
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Functions are available via dot-sourcing (no Export-ModuleMember needed)
