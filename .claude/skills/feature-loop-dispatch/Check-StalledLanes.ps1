# Check-StalledLanes.ps1
# Feature-loop stall watchdog. Detects lanes that stopped making progress without anyone noticing.
# Safe: read-only against GitHub (REST only). Exit code 0 = no stalls, 2 = stalls found.
#
# Usage:
#   pwsh -NoProfile ./Check-StalledLanes.ps1 -Repo ClearMeasure/Clear-Measure-Intelligence-Scorecard [-StaleMinutes 15] [-Json]
#
# Detects, per open PR:
#   GREEN_UNMERGED  - every check-run 'success' for > StaleMinutes, PR still open (stalled at merge step)
#   DIRTY           - PR has merge conflicts (needs master re-merged into branch)
#   CI_FAILED       - one or more check-runs concluded failure/cancelled/timed_out (needs fix+repush)
#   CI_STUCK        - newest check-run started > 60 min ago and still not completed
# And per recently-merged PR (last 30):
#   MERGED_ISSUE_OPEN - PR body says "Closes #N" (or branch names issue N) but issue N is still open
#                       > StaleMinutes after merge (missing closing keyword / stalled closeout)

param(
    [Parameter(Mandatory = $true)][string]$Repo,
    [int]$StaleMinutes = 15,
    # Directory containing subagent task .output files; when provided, lanes whose output
    # file hasn't been touched in LocalStaleMinutes are flagged LOCAL_STALL (catches stalls
    # in the pre-PR phase that GitHub state can't show).
    [string]$TasksDir,
    # Comma-separated task/agent IDs that are currently ACTIVE; only these are checked for
    # LOCAL_STALL. Without it every historical completed task file would be flagged.
    [string[]]$ActiveIds = @(),
    [int]$LocalStaleMinutes = 25,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$now = [DateTimeOffset]::UtcNow
$stalls = @()

function Get-CheckSummary([string]$sha) {
    $runs = gh api "repos/$Repo/commits/$sha/check-runs" --paginate --jq '[.check_runs[] | {name, status, conclusion, completed_at, started_at}]' | ConvertFrom-Json
    if (-not $runs) { return $null }
    $total      = $runs.Count
    $success    = @($runs | Where-Object { $_.conclusion -eq 'success' }).Count
    $failed     = @($runs | Where-Object { $_.conclusion -in @('failure', 'cancelled', 'timed_out', 'action_required') }).Count
    $incomplete = @($runs | Where-Object { $_.status -ne 'completed' }).Count
    $lastDone   = ($runs | Where-Object { $_.completed_at } | Sort-Object completed_at | Select-Object -Last 1).completed_at
    $firstStart = ($runs | Where-Object { $_.started_at } | Sort-Object started_at | Select-Object -First 1).started_at
    [pscustomobject]@{
        Total = $total; Success = $success; Failed = $failed; Incomplete = $incomplete
        LastCompletedAt = if ($lastDone) { [DateTimeOffset]$lastDone } else { $null }
        FirstStartedAt  = if ($firstStart) { [DateTimeOffset]$firstStart } else { $null }
        FailedNames = @($runs | Where-Object { $_.conclusion -in @('failure', 'cancelled', 'timed_out', 'action_required') } | ForEach-Object name)
    }
}

# ---- Open PRs ----
$openPrs = gh pr list -R $Repo --state open --limit 50 --json number,headRefName,headRefOid,mergeStateStatus,updatedAt,title | ConvertFrom-Json
foreach ($pr in $openPrs) {
    $ci = Get-CheckSummary $pr.headRefOid
    if (-not $ci) {
        $stalls += [pscustomobject]@{ Kind = 'CI_STUCK'; PR = $pr.number; Branch = $pr.headRefName; Detail = 'No check-runs found on head commit (push may not have triggered CI).' }
        continue
    }
    if ($ci.Failed -gt 0) {
        $stalls += [pscustomobject]@{ Kind = 'CI_FAILED'; PR = $pr.number; Branch = $pr.headRefName; Detail = "Failed jobs: $($ci.FailedNames -join ', '). Needs fix + re-push." }
        continue
    }
    if ($ci.Incomplete -gt 0) {
        if ($ci.FirstStartedAt -and (($now - $ci.FirstStartedAt).TotalMinutes -gt 60)) {
            $stalls += [pscustomobject]@{ Kind = 'CI_STUCK'; PR = $pr.number; Branch = $pr.headRefName; Detail = "CI running > 60 min ($($ci.Incomplete) job(s) incomplete)." }
        }
        continue  # CI still running within tolerance — not a stall
    }
    if ($ci.Success -eq $ci.Total -and $ci.Total -gt 0) {
        $greenAge = if ($ci.LastCompletedAt) { ($now - $ci.LastCompletedAt).TotalMinutes } else { [double]::MaxValue }
        if ($pr.mergeStateStatus -eq 'DIRTY') {
            $stalls += [pscustomobject]@{ Kind = 'DIRTY'; PR = $pr.number; Branch = $pr.headRefName; Detail = 'Merge conflicts with base. Re-merge master into branch, rebuild, re-push.' }
        }
        elseif ($greenAge -gt $StaleMinutes) {
            $stalls += [pscustomobject]@{ Kind = 'GREEN_UNMERGED'; PR = $pr.number; Branch = $pr.headRefName; Detail = ('All {0} checks green for {1:n0} min but PR not merged. Lane stalled at merge/triage step.' -f $ci.Total, $greenAge) }
        }
    }
}

# ---- Recently merged PRs whose linked issue is still open ----
$mergedPrs = gh pr list -R $Repo --state merged --limit 30 --json number,body,headRefName,mergedAt | ConvertFrom-Json
foreach ($pr in $mergedPrs) {
    $mergedAge = ($now - [DateTimeOffset]$pr.mergedAt).TotalMinutes
    if ($mergedAge -le $StaleMinutes) { continue }
    $issueNums = @()
    if ($pr.body) {
        $issueNums += [regex]::Matches($pr.body, '(?i)\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+#(\d+)') | ForEach-Object { $_.Groups[1].Value }
    }
    if ($pr.headRefName -match '(?:issue|fix)[/\-](\d+)') { $issueNums += $Matches[1] }
    foreach ($n in ($issueNums | Select-Object -Unique)) {
        try { $state = gh api "repos/$Repo/issues/$n" --jq .state 2>$null } catch { continue }
        if ($state -eq 'open') {
            $subs = gh api "repos/$Repo/issues/$n/sub_issues" --jq '[.[] | select(.state == "open")] | length' 2>$null
            if ([int]$subs -gt 0) { continue }  # deliberately open: clamped behind open children
            $stalls += [pscustomobject]@{ Kind = 'MERGED_ISSUE_OPEN'; PR = $pr.number; Branch = $pr.headRefName; Detail = ('PR merged {0:n0} min ago but issue #{1} is still open with no open children (missing closing keyword or stalled closeout).' -f $mergedAge, $n) }
        }
    }
}

# ---- Local lane liveness (pre-PR phase, invisible to GitHub) ----
if ($TasksDir -and (Test-Path $TasksDir) -and $ActiveIds.Count -gt 0) {
    $cutoff = (Get-Date).AddMinutes(-$LocalStaleMinutes)
    foreach ($id in $ActiveIds) {
        $f = Get-Item -Path (Join-Path $TasksDir "$id.output") -ErrorAction SilentlyContinue
        if (-not $f) {
            $stalls += [pscustomobject]@{ Kind = 'LOCAL_STALL'; PR = 0; Branch = $id; Detail = 'Active lane has no task output file at all.' }
        }
        elseif ($f.LastWriteTime -lt $cutoff) {
            $stalls += [pscustomobject]@{
                Kind = 'LOCAL_STALL'; PR = 0; Branch = $id
                Detail = ('ACTIVE lane output untouched since {0:HH:mm:ss} (> {1} min) - stalled pre-PR.' -f $f.LastWriteTime, $LocalStaleMinutes)
            }
        }
    }
}

if ($Json) {
    $stalls | ConvertTo-Json -Depth 4
}
else {
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')
    if ($stalls.Count -eq 0) {
        Write-Output "[$stamp] OK - no stalled lanes detected in $Repo."
    }
    else {
        Write-Output "[$stamp] $($stalls.Count) STALL(S) detected in ${Repo}:"
        $stalls | ForEach-Object { Write-Output ("  [{0}] PR #{1} ({2}) - {3}" -f $_.Kind, $_.PR, $_.Branch, $_.Detail) }
    }
}
exit ($stalls.Count -gt 0 ? 2 : 0)
