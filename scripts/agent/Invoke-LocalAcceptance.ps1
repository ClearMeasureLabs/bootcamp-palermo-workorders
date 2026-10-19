# v41g: build AcceptanceTests (always on src changes) and, when UI/acceptance files changed, run the affected acceptance classes
# locally on SQLite + headless Playwright (round 27: c41-c43 acceptance REDs/compile errors were found only in CI, 2-3 cycles each).
# Prints ACC_BUILD=FAIL / FIX: lines + ACC_TEST=FIX, ACC_TEST=OK, or ACC_TEST=SKIPPED <reason>. Tool problems never fail the caller.
$ErrorActionPreference = 'Continue'
Set-Location (git rev-parse --show-toplevel)
$chg = @(git status --porcelain -uall -- src | ForEach-Object { $_.Substring(3) })
$proj = 'src/AcceptanceTests/AcceptanceTests.csproj'
# v43g: the CI-volume seeding hook is a temp patch applied here and always reverted (v42g committed it to src/, and it leaked into
# every round 29 PR and hid c61/c62's empty changes from NO_CHANGES)
$seed = "$(git rev-parse --show-toplevel)/scripts/agent/templates/acc-seed.patch"; $seeded = $false
if ((Test-Path $seed) -and -not (Select-String -Quiet 'AGENT_ACC_EXTRA_WORKORDERS' src/AcceptanceTests/ServerFixture.cs)) { git apply $seed 2>$null; $seeded = -not $LASTEXITCODE }
try {
$b = dotnet build $proj -c Release -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String ': error ' | ForEach-Object { "FIX: $($_.Line.Trim() -replace '\s*\[.*\]$')" } | Select-Object -Unique -First 10; 'ACC_BUILD=FAIL'; return }
$ui = @($chg | Where-Object { $_ -match '^src/(UI|UI\.Shared|UI\.Client|AcceptanceTests)/' -and $_ -match '\.(razor|cs|css)$' })
if (-not $ui) { 'ACC_TEST=SKIPPED no UI/acceptance change'; return }
$cls = @($chg | Where-Object { $_ -match '^src/AcceptanceTests/.*Tests\.cs$' -and (Test-Path $_) } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
$pages = @($ui | Where-Object { $_ -notmatch '^src/AcceptanceTests/' -and $_ -match '\.razor(\.cs)?$' } | ForEach-Object { (Split-Path $_ -Leaf).Split('.')[0] } | Select-Object -Unique)
# v42g: a changed non-page UI .cs (state, service, model) maps to the pages that use it (round 28 c53: WorkOrderSearchState.cs -> SKIPPED)
$types = @($ui | Where-Object { $_ -notmatch '^src/AcceptanceTests/' -and $_ -match '\.cs$' -and $_ -notmatch '\.razor\.cs$' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
if ($types) { $pages += @(rg -l -w @($types | ForEach-Object { '-e'; $_ }) --glob '*.razor' --glob '*.razor.cs' src/UI.Shared src/UI 2>$null | ForEach-Object { (Split-Path $_ -Leaf).Split('.')[0] }); $pages = @($pages | Select-Object -Unique) }
if ($pages) { $cls += @(rg -l -w @($pages | ForEach-Object { '-e'; $_ }) --glob '*Tests.cs' src/AcceptanceTests 2>$null | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }) }
$cls = @($cls | Where-Object { $_ } | Select-Object -Unique -First 6)
# v42g: the shared test base, or a search/manage default (state class, or an added initializer/OnInitialized line in those pages),
# reaches every flow that lands on those pages: run all WorkOrders + McpServer classes (round 28 c53: 3 CI REDs across 5 classes)
$wide = @($chg | Where-Object { $_ -match 'AcceptanceTestBase\.cs$|^src/UI\.Shared/.*(State|Defaults)\w*\.cs$' }).Count -gt 0
if (-not $wide) { foreach ($p in $ui | Where-Object { $_ -match '/WorkOrder(Search|Manage)\.razor(\.cs)?$' }) {
  if (git diff -U0 -- $p | Where-Object { $_ -match '^\+.*(\{ get; set; \} = |OnInitialized|OnParametersSet)' }) { $wide = $true } } }
if (-not $cls -and -not $wide) { 'ACC_TEST=SKIPPED no acceptance class covers the changed files'; return }
$pw = "src/AcceptanceTests/bin/Release/net10.0/playwright.ps1"
if (-not (Get-ChildItem "$HOME/.cache/ms-playwright" -Filter 'chromium*' -ErrorAction SilentlyContinue)) {
  pwsh -NoProfile $pw install chromium --with-deps 2>&1 | Out-Null
  if ($LASTEXITCODE) { pwsh -NoProfile $pw install chromium 2>&1 | Out-Null }
  if ($LASTEXITCODE) { 'ACC_TEST=SKIPPED playwright install failed'; return } }
$db = Join-Path ([IO.Path]::GetTempPath()) 'agent-acc.db'; Remove-Item $db -ErrorAction SilentlyContinue
$env:DATABASE_ENGINE = 'SQLite'; $env:ConnectionStrings__SqlConnectionString = "Data Source=$db"
$f = ($cls | ForEach-Object { "FullyQualifiedName~.$_." }) -join '|'
if ($wide) { $f = (@('FullyQualifiedName~.AcceptanceTests.WorkOrders.', 'FullyQualifiedName~.AcceptanceTests.McpServer.') + @($cls | ForEach-Object { "FullyQualifiedName~.$_." })) -join '|'; $cls = @('WorkOrders/*', 'McpServer/*') + $cls }
if ($seeded) { $env:AGENT_ACC_EXTRA_WORKORDERS = '40' }  # v42g: CI-like row volume (ServerFixture seeds it; round 28 c49 paging RED)
$r = dotnet test $proj -c Release --no-build --nologo --settings src/AcceptanceTests/AcceptanceTests.runsettings --logger 'console;verbosity=normal' --filter $f 2>&1
$code = $LASTEXITCODE; Remove-Item env:DATABASE_ENGINE, env:ConnectionStrings__SqlConnectionString, env:AGENT_ACC_EXTRA_WORKORDERS
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
} finally { if ($seeded) { git apply -R $seed 2>$null } }
