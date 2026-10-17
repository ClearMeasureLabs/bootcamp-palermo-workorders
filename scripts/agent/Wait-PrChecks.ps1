# v3. Wait for every check-run on the PR head and print a compact verdict. Each call returns within -MaxCallMin
# (default 9, under the agent runtime's ~10 min terminal cap, so the call never gets backgrounded):
#   exit 0 CHECKS=GREEN | 1 CHECKS=RED (code failures, with log tail) | 3 CHECKS=PENDING (just rerun the same command)
# Infra flakes (a job cancelled or timed out, or running >= 25 min with no test report) are re-run ONCE automatically
# with `gh run rerun --failed` instead of reported. That avoids empty-commit retriggers and log spelunking.
# The aggregator "Build result" is not reported as a root cause.
param([Parameter(Mandatory)][int]$Pr, [int]$MaxCallMin = 9, [int]$PollSec = 45, [int]$SettleSec = 60,
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$deadline = (Get-Date).AddMinutes($MaxCallMin)
$state = Join-Path ([IO.Path]::GetTempPath()) "wait-pr-$Pr.json"
$rerun = if (Test-Path $state) { Get-Content $state -Raw | ConvertFrom-Json } else { @() }
$sha = gh pr view $Pr --repo $Repo --json headRefOid -q .headRefOid
$age = (Get-Date) - [datetime](gh api "repos/$Repo/commits/$sha" --jq .commit.committer.date)
if ($age.TotalSeconds -lt $SettleSec) { Start-Sleep -Seconds ($SettleSec - [int]$age.TotalSeconds) }
function Get-Runs {
  gh api "repos/$Repo/commits/$sha/check-runs?per_page=100" --jq '.check_runs[] | [.name, .status, (.conclusion // ""), (.details_url // ""), (.started_at // ""), (.completed_at // "")] | @tsv' |
    ForEach-Object { $f = $_ -split "`t"; [pscustomobject]@{ Name = $f[0]; Status = $f[1]; Conclusion = $f[2]; Url = $f[3]; Start = $f[4]; End = $f[5] } }
}
while ($true) {
  $runs = @(Get-Runs)
  $pending = @($runs | Where-Object Status -ne 'completed')
  $bad = @($runs | Where-Object { $_.Status -eq 'completed' -and $_.Conclusion -notin 'success', 'skipped', 'neutral' -and $_.Name -ne 'Build result' })
  # Flake triage: rerun once per run id.
  foreach ($b in $bad) {
    if ($b.Url -notmatch '/runs/(\d+)') { continue }; $runId = $Matches[1]
    $mins = if ($b.Start -and $b.End) { ([datetime]$b.End - [datetime]$b.Start).TotalMinutes } else { 0 }
    $log = (gh run view $runId --repo $Repo --log-failed 2>$null) -replace "`0", ''
    $flaky = $b.Conclusion -in 'cancelled', 'timed_out' -or ($mins -ge 25 -and ($log -match 'No test report files were found|No files were found'))
    if ($flaky -and $runId -notin $rerun) {
      gh run rerun $runId --repo $Repo --failed 2>&1 | Out-Null
      $rerun = @($rerun) + $runId; $rerun | ConvertTo-Json | Set-Content $state
      "FLAKE-RERUN: $($b.Name) ($($b.Conclusion), $([int]$mins) min) run $runId"
      $pending = @('rerun')
    }
  }
  if ($runs -and -not $pending) { break }
  if ((Get-Date).AddSeconds($PollSec) -gt $deadline) {
    'CHECKS=PENDING'; exit 3
  }
  Start-Sleep -Seconds $PollSec
}
if (-not $bad) { "RESULT pr=$Pr head=$sha CHECKS=GREEN"; Remove-Item $state -ErrorAction SilentlyContinue; exit 0 }
"HEAD=$sha FAILED=$($bad.Count)"
foreach ($b in $bad) {
  "--- FAILED: $($b.Name) ($($b.Conclusion))"
  if ($b.Url -match '/runs/(\d+)') {
    ((gh run view $Matches[1] --repo $Repo --log-failed 2>$null) -replace "`0", '') -split "`n" |
      Select-String -Pattern 'error [A-Z]+\d+|Failed |Assert|Exception|error:' | Select-Object -Last 20 |
      ForEach-Object { $l = $_.Line -replace '^\S+\s+\S+\s+\S+Z\s', ''; $l.Substring(0, [Math]::Min(250, $l.Length)) }
  }
}
'CHECKS=RED'; exit 1
