# v41g: build AcceptanceTests (always on src changes) and, when UI/acceptance files changed, run the affected acceptance classes
# locally on SQLite + headless Playwright (round 27: c41-c43 acceptance REDs/compile errors were found only in CI, 2-3 cycles each).
# Prints ACC_BUILD=FAIL / FIX: lines + ACC_TEST=FIX, ACC_TEST=OK, or ACC_TEST=SKIPPED <reason>. Tool problems never fail the caller.
$ErrorActionPreference = 'Continue'
Set-Location (git rev-parse --show-toplevel)
$chg = @(git status --porcelain -uall -- src | ForEach-Object { $_.Substring(3) })
$proj = 'src/AcceptanceTests/AcceptanceTests.csproj'
$b = dotnet build $proj -c Release -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String ': error ' | ForEach-Object { "FIX: $($_.Line.Trim() -replace '\s*\[.*\]$')" } | Select-Object -Unique -First 10; 'ACC_BUILD=FAIL'; return }
$ui = @($chg | Where-Object { $_ -match '^src/(UI|UI\.Shared|UI\.Client|AcceptanceTests)/' -and $_ -match '\.(razor|cs|css)$' })
if (-not $ui) { 'ACC_TEST=SKIPPED no UI/acceptance change'; return }
$cls = @($chg | Where-Object { $_ -match '^src/AcceptanceTests/.*Tests\.cs$' -and (Test-Path $_) } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
$pages = @($ui | Where-Object { $_ -notmatch '^src/AcceptanceTests/' -and $_ -match '\.razor(\.cs)?$' } | ForEach-Object { (Split-Path $_ -Leaf).Split('.')[0] } | Select-Object -Unique)
if ($pages) { $cls += @(rg -l -w @($pages | ForEach-Object { '-e'; $_ }) --glob '*Tests.cs' src/AcceptanceTests 2>$null | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }) }
$cls = @($cls | Where-Object { $_ } | Select-Object -Unique -First 6)
if (-not $cls) { 'ACC_TEST=SKIPPED no acceptance class covers the changed files'; return }
$pw = "src/AcceptanceTests/bin/Release/net10.0/playwright.ps1"
if (-not (Get-ChildItem "$HOME/.cache/ms-playwright" -Filter 'chromium*' -ErrorAction SilentlyContinue)) {
  pwsh -NoProfile $pw install chromium --with-deps 2>&1 | Out-Null
  if ($LASTEXITCODE) { pwsh -NoProfile $pw install chromium 2>&1 | Out-Null }
  if ($LASTEXITCODE) { 'ACC_TEST=SKIPPED playwright install failed'; return } }
$db = Join-Path ([IO.Path]::GetTempPath()) 'agent-acc.db'; Remove-Item $db -ErrorAction SilentlyContinue
$env:DATABASE_ENGINE = 'SQLite'; $env:ConnectionStrings__SqlConnectionString = "Data Source=$db"
$f = ($cls | ForEach-Object { "FullyQualifiedName~.$_." }) -join '|'
$r = dotnet test $proj -c Release --no-build --nologo --settings src/AcceptanceTests/AcceptanceTests.runsettings --logger 'console;verbosity=normal' --filter $f 2>&1
$code = $LASTEXITCODE; Remove-Item env:DATABASE_ENGINE, env:ConnectionStrings__SqlConnectionString
$sum = @($r | Select-String 'Passed!|Failed!|Total tests|Passed:' | ForEach-Object { $_.Line.Trim() }) | Select-Object -Last 1
if (-not $code) { "ACC_TEST=OK ($($cls -join ',')) $($sum -replace ' - .*$')"; return }
$fail = @($r | Select-String '^\s*Failed (\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -Unique)
if (-not $fail) {
  if ($r -match 'Executable doesn''t exist|Host system is missing dependencies|browserType.launch') { 'ACC_TEST=SKIPPED browser cannot start on this VM'; return }
  $r | Select-String 'error|Exception' | Select-Object -First 5 | ForEach-Object { "FIX: acceptance run: $($_.Line.Trim())" }; 'ACC_TEST=FIX'; return }
foreach ($t in $fail | Select-Object -First 6) {
  $i = [array]::FindIndex([string[]]$r, [Predicate[string]]{ param($l) $l -match "^\s*Failed $([regex]::Escape($t))" })
  $msg = ($r[($i + 1)..($i + 6)] | Where-Object { $_ -and $_ -notmatch '^\s*(Error Message:|Stack Trace:)\s*$' } | Select-Object -First 3 | ForEach-Object { $_.Trim() }) -join ' | '
  "FIX: acceptance $($t -replace '^.*\.(\w+\.\w+)$','$1'): $($msg.Substring(0, [Math]::Min(300, $msg.Length)))" }
"ACC_TEST=FIX ($($fail.Count) failed; run on SQLite + headless Chromium like CI)"
