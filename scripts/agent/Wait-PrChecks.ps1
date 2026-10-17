# One blocking call: wait until every check-run on the PR head finishes, then print a compact summary and,
# for failures, the tail of the failed job logs. Exit 0 = all green (success/skipped/neutral).
param([Parameter(Mandatory)][int]$Pr, [int]$TimeoutMin = 40, [int]$PollSec = 60, [int]$SettleSec = 90,
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$deadline = (Get-Date).AddMinutes($TimeoutMin)
Start-Sleep -Seconds $SettleSec   # let workflows register their check-runs
do {
  $sha = gh pr view $Pr --repo $Repo --json headRefOid -q .headRefOid
  $runs = gh api "repos/$Repo/commits/$sha/check-runs?per_page=100" --jq '.check_runs[] | [.name, .status, (.conclusion // ""), (.details_url // "")] | @tsv' |
    ForEach-Object { $f = $_ -split "`t"; [pscustomobject]@{ Name = $f[0]; Status = $f[1]; Conclusion = $f[2]; Url = $f[3] } }
  $pending = @($runs | Where-Object Status -ne 'completed')
  if ($runs -and -not $pending) { break }
  Start-Sleep -Seconds $PollSec
} while ((Get-Date) -lt $deadline)
$bad = @($runs | Where-Object { $_.Status -eq 'completed' -and $_.Conclusion -notin 'success', 'skipped', 'neutral' })
"HEAD=$sha TOTAL=$(@($runs).Count) PENDING=$(@($pending).Count) FAILED=$($bad.Count)"
if ($pending) { 'TIMEOUT pending: ' + (($pending | ForEach-Object Name) -join ', '); exit 2 }
if (-not $bad) { 'CHECKS=GREEN'; exit 0 }
foreach ($b in $bad) {
  "--- FAILED: $($b.Name) ($($b.Conclusion))"
  if ($b.Url -match '/runs/(\d+)') {
    gh run view $Matches[1] --repo $Repo --log-failed 2>$null | Select-String -Pattern 'error|fail|Assert|Exception' | Select-Object -Last 25 | ForEach-Object { $_.Line.Substring(0, [Math]::Min(300, $_.Line.Length)) }
  }
}
'CHECKS=RED'; exit 1
