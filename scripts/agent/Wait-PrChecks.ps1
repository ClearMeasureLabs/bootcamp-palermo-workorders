# v11 (PENDING exits 0). v10 (stuck-queued ignore). v3. Wait for every check-run on the PR head and print a compact verdict. Each call returns within -MaxCallMin
# (default 9, under the agent runtime's ~10 min terminal cap, so the call never gets backgrounded):
#   exit 0 CHECKS=GREEN | 1 CHECKS=RED (code failures, with log tail) | 3 CHECKS=PENDING (just rerun the same command)
# Infra flakes (a job cancelled or timed out, or running >= 25 min with no test report) are re-run ONCE automatically
# with `gh run rerun --failed` instead of reported. That avoids empty-commit retriggers and log spelunking.
# The aggregator "Build result" is not reported as a root cause.
param([Parameter(Mandatory)][int]$Pr, [switch]$ForceRerun, [int]$MaxCallMin = 9, [int]$PollSec = 45, [int]$SettleSec = 60,
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$deadline = (Get-Date).AddMinutes($MaxCallMin)
$state = Join-Path ([IO.Path]::GetTempPath()) "wait-pr-$Pr.json"
$rerun = @(if (Test-Path $state) { Get-Content $state -Raw | ConvertFrom-Json } else { @() })
$graceUntil = Get-Date  # v8: after a rerun, old failed check-runs stay "completed" for a bit; don't report RED from them (rep-3 t02)
$sha = gh api "repos/$Repo/pulls/$Pr" --jq .head.sha  # REST (gh pr view uses GraphQL)
$age = (Get-Date) - [datetime](gh api "repos/$Repo/commits/$sha" --jq .commit.committer.date)
if ($age.TotalSeconds -lt $SettleSec) { Start-Sleep -Seconds ($SettleSec - [int]$age.TotalSeconds) }
function Get-Runs {
  gh api "repos/$Repo/commits/$sha/check-runs?per_page=100" --jq '.check_runs[] | [.name, .status, (.conclusion // ""), (.details_url // ""), (.started_at // ""), (.completed_at // "")] | @tsv' |
    ForEach-Object { $f = $_ -split "`t"; [pscustomobject]@{ Name = $f[0]; Status = $f[1]; Conclusion = $f[2]; Url = $f[3]; Start = $f[4]; End = $f[5] } }
}
while ($true) {
  $runs = @(Get-Runs)
  $pending = @($runs | Where-Object Status -ne 'completed')
  # v10: a check that never leaves 'queued' (phase 4 rep 1 g04: QDNETC queued >1 h after every other check finished) is
  # reported as IGNORED_QUEUED once nothing else is running and it has been queued >= 15 min, instead of PENDING forever.
  if ($pending -and -not @($pending | Where-Object { $_.Status -ne 'queued' }) -and @($runs | Where-Object Status -eq 'completed')) {
    $stuck = @($pending | Where-Object { $_.Start -and ((Get-Date).ToUniversalTime() - ([datetime]$_.Start).ToUniversalTime()).TotalMinutes -ge 15 })
    if ($stuck.Count -eq $pending.Count) { $stuck | ForEach-Object { "IGNORED_QUEUED: $($_.Name)" }; $pending = @() }
  }
  $bad = @($runs | Where-Object { $_.Status -eq 'completed' -and $_.Conclusion -notin 'success', 'skipped', 'neutral' -and $_.Name -ne 'Build result' })
  # Flake triage: rerun once per run id.
  foreach ($b in $bad) {
    if ($b.Url -notmatch '/runs/(\d+)') { continue }; $runId = $Matches[1]
    $mins = if ($b.Start -and $b.End) { ([datetime]$b.End - [datetime]$b.Start).TotalMinutes } else { 0 }
    $log = (gh run view $runId --repo $Repo --log-failed 2>$null) -replace "`0", ''
    # v8: acceptance/integration jobs fail intermittently on tests the PR doesn't touch (rep-3 t09, t11), so rerun them once.
    # A real failure reproduces on the rerun and is then reported. -ForceRerun forces one rerun of any failed job.
    $flaky = $ForceRerun -or $b.Conclusion -in 'cancelled', 'timed_out' -or $b.Name -match 'Acceptance|Integration' -or ($mins -ge 25 -and ($log -match 'No test report files were found|No files were found'))
    if ($flaky -and $runId -notin $rerun) {
      # v9: `gh run rerun` fails while other jobs of the run are still in progress. v8 recorded the run as rerun anyway, so neither the
      # auto rerun nor -ForceRerun ever happened (rep-5 t04). Record it only on success; otherwise keep polling and retry later.
      gh run rerun $runId --repo $Repo --failed 2>&1 | Out-Null
      if ($LASTEXITCODE) { if ($pending.Count) { continue } else { "RERUN_FAILED: run $runId"; continue } }  # defer only while other jobs still run
      $rerun = @($rerun) + $runId; $rerun | ConvertTo-Json | Set-Content $state
      "FLAKE-RERUN: $($b.Name) ($($b.Conclusion), $([int]$mins) min) run $runId"
      $pending = @('rerun'); $graceUntil = (Get-Date).AddSeconds(120)
    }
  }
  if ($runs -and -not $pending -and (Get-Date) -gt $graceUntil) { break }
  if ((Get-Date).AddSeconds($PollSec) -gt $deadline) {
    "CHECKS=PENDING (run the same command again)"; exit 0  # v11: exit 0 so the output is recorded; was exit 3
  }
  Start-Sleep -Seconds $PollSec
}
if (-not $bad) { "CHECKS=GREEN"; $(if (Test-Path (Join-Path ([IO.Path]::GetTempPath()) 'agent-multi-item')) { "NEXT: print RESULT=GREEN PR=https://github.com/$Repo/pull/$Pr on its own line, then the next item (Start-NextItem.ps1); after the last item's RESULT, stop: no tool calls" } else { "NEXT: print RESULT=GREEN PR=https://github.com/$Repo/pull/$Pr  (your final reply is exactly that line; after RESULT, stop: no tool calls)" }); Remove-Item $state -ErrorAction SilentlyContinue; exit 0 }
"HEAD=$sha FAILED=$($bad.Count)"
foreach ($b in $bad) {
  "--- FAILED: $($b.Name) ($($b.Conclusion))"
  if ($b.Url -match '/runs/(\d+)') {
    ((gh run view $Matches[1] --repo $Repo --log-failed 2>$null) -replace "`0", '') -split "`n" |
      Select-String -Pattern 'error [A-Z]+\d+|Failed |Assert|Exception|error:|CRAP|exceed|violation|threshold' | Select-Object -Last 20 |
      ForEach-Object { $l = $_.Line -replace '^\S+\s+\S+\s+\S+Z\s', ''; $l.Substring(0, [Math]::Min(250, $l.Length)) }
  }
  if (-not $qdone -and $b.Url -match '/runs/(\d+)') { $qdone = $true  # v7: Qodana findings live in the SARIF artifact, not the log
    $d = Join-Path ([IO.Path]::GetTempPath()) "qodana-$($Matches[1])"; Remove-Item -Recurse -Force $d -ErrorAction SilentlyContinue
    gh run download $Matches[1] --repo $Repo -p 'qodana*' -D $d 2>$null | Out-Null
    Get-ChildItem $d -Recurse -Filter *.zip -ErrorAction SilentlyContinue | ForEach-Object { Expand-Archive $_.FullName -DestinationPath "$d/x" -Force }
    $sar = Get-ChildItem $d -Recurse -Filter 'qodana.sarif.json' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sar) { (Get-Content $sar.FullName -Raw | ConvertFrom-Json).runs[0].results | Where-Object { $_.baselineState -eq 'new' } | Select-Object -First 10 |
      ForEach-Object { "QODANA $($_.ruleId) $($_.locations[0].physicalLocation.artifactLocation.uri):$($_.locations[0].physicalLocation.region.startLine) $($_.message.text)" } }
  }
}
'CHECKS=RED'; exit 0  # v19g: exit 0 so the transcript keeps the output
