# v15g: inline CI wait removed (round 1: Submit + wait outran the terminal cap, output was lost and agents went digging).
# v14g: Risk check (Test-ChangeRisk: CRAP/Qodana/Number traps that caused past CI REDs) ->
# full unit suite + IntegrationTests build -> changed integration test classes on SQLite -> publish the workspace src diff
# (temp worktree on master; rerun pushes a follow-up commit) -> waits for CI inside the same call (~9 min budget) and
# prints the verdict. If CI is still running it prints WAIT_CMD. -IgnoreRisk skips a RISK you judge false.
# -VerifyOnly = local checks only. (v9g-v11g history: WAIT_CMD 9-min calls, PENDING exits 0.)
param([int]$Issue, [string]$Branch, [switch]$VerifyOnly, [switch]$IgnoreRisk, [string]$Filter = '',
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
# v26g: name the script line on any crash so SCRIPT_FAIL is diagnosable
trap { "SCRIPT_ERROR $($_.InvocationInfo.ScriptName | Split-Path -Leaf):$($_.InvocationInfo.ScriptLineNumber) $($_.Exception.Message)"; exit 0 }  # exit 0: non-zero exits lose the output in transcripts
Set-Location (git rev-parse --show-toplevel)  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
$t0 = Get-Date
$tools = Join-Path ([IO.Path]::GetTempPath()) 'agent-scripts'
New-Item -ItemType Directory -Force $tools | Out-Null; Copy-Item "$PSScriptRoot/*.ps1" $tools -Force
# v33g: a changed .proto regenerates the checked-in gRPC C# first (round 19 s08 hand-edited it: 77 edits, 1.71M tokens)
# v36g: non-code change (only .github/docs/root *.ps1): skip risk/build/tests, syntax-check instead, publish (round 22 c04)
$srcChg = git status --porcelain -uall -- src; $nonSrc = @(git status --porcelain -uall -- .github docs ':(glob)*.ps1' | ForEach-Object { $_.Substring(3) })
$nonCode = -not $srcChg -and $nonSrc
if ($nonCode) {
  python3 -c 'import yaml' 2>$null; $hasYaml = $LASTEXITCODE -eq 0
  foreach ($p in $nonSrc | Where-Object { Test-Path $_ }) {
    if ($p -like '*.ps1') { $e = $null; [void][Management.Automation.Language.Parser]::ParseFile((Resolve-Path $p), [ref]$null, [ref]$e); if ($e) { "SYNTAX_FAIL ${p}: $($e[0].Message) line $($e[0].Extent.StartLineNumber)"; exit 0 } }
    if ($p -match '\.ya?ml$' -and $hasYaml) { $y = python3 -c "import sys,yaml;yaml.safe_load(open(sys.argv[1]))" $p 2>&1; if ($LASTEXITCODE) { "SYNTAX_FAIL ${p}: $($y | Select-Object -Last 1)"; exit 0 } } }
  'NON_CODE: syntax OK; .NET build/tests skipped' }
if (git status --porcelain -- 'src/**/*.proto') { & "$tools/Update-GrpcGenerated.ps1" }
$risk = if ($nonCode) { @() } else { @(& "$tools/Test-ChangeRisk.ps1") }
if ($risk -and -not $IgnoreRisk) { $risk; 'RISK=FIX (fix these, rerun; -IgnoreRisk only if clearly false)'; exit 0 }
if (-not $nonCode) {
$f = if ($Filter) { $Filter } else { 'FullyQualifiedName!~__none__' }
$u = @(& "$tools/Invoke-AgentBuildTest.ps1" -Filter $f); if ($LASTEXITCODE) { $u | Where-Object { $_ -notmatch '^(BUILD=OK)$' }; 'SUBMIT=FAIL (fix the items above, rerun Submit)'; exit 0 }
$b = dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String 'error' | Select-Object -First 10; 'IT_BUILD=FAIL'; exit 0 }
$it = @(git status --porcelain -uall -- src/IntegrationTests | ForEach-Object { $_.Substring(3) } | Where-Object { $_ -like '*.cs' } |
  ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
# v33g: also run integration test classes that mention a changed production type (round 19 s02: an untouched integration
# test asserted the old behavior and only failed in CI)
$types = @(git status --porcelain -uall -- src | ForEach-Object { $_.Substring(3) } | Where-Object { $_ -like '*.cs' -and $_ -notmatch 'Tests[/\\]|Generated' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
if ($types) { $it = @(@($it) + @(rg -l -w @($types | ForEach-Object { '-e'; $_ }) --glob '*Tests.cs' src/IntegrationTests 2>$null | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }) | Where-Object { $_ } | Select-Object -Unique -First 8) }
if ($it) {
  $db = Join-Path ([IO.Path]::GetTempPath()) 'agent-it.db'; Remove-Item $db -ErrorAction SilentlyContinue
  $env:DATABASE_ENGINE = 'SQLite'; $env:ConnectionStrings__SqlConnectionString = "Data Source=$db"
  $r = dotnet test src/IntegrationTests/IntegrationTests.csproj -c Release --no-build --nologo -v q --logger 'console;verbosity=minimal' --filter (($it | ForEach-Object { "FullyQualifiedName~$_" }) -join '|') 2>&1
  $code = $LASTEXITCODE; Remove-Item env:DATABASE_ENGINE, env:ConnectionStrings__SqlConnectionString
  if ($code) { $r | Select-String 'Failed |Assert|Expected|Exception|error|Status' | Select-Object -First 20; 'IT_TEST=FAIL (SQLite)'; exit 0 }
  'IT_TEST=OK'
}
}
if ($VerifyOnly) { 'VERIFY=OK'; exit 0 }
if (-not $Issue -or -not $Branch) { 'need -Issue and -Branch to publish'; exit 1 }
$title = gh api "repos/$Repo/issues/$Issue" --jq .title
$out = & "$tools/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
if ($out -match 'no src changes' -and -not $nonCode) { 'NO_CHANGES: nothing to publish. If the criteria are already met, reply RESULT=ALREADY_DONE <file:line evidence>'; exit 0 }
if ($out -match 'PUBLISH_FAIL') { $out; exit 0 }
$out | Where-Object { $_ -match 'NO_CHANGE' }
# v26g: never index a missing match (round 12 s03 "Cannot index into a null array."); fall back to a REST lookup.
$m = @($out | Select-String 'pull/(\d+)')
$pr = if ($m) { [int]$m[0].Matches[0].Groups[1].Value } else { [int](gh api "repos/$Repo/pulls?head=$($Repo.Split('/')[0]):$Branch&state=open" --jq '.[0].number // 0' 2>$null) }
if (-not $pr) { $out; 'PUBLISH_FAIL: no PR number found (rerun Submit once; if again, reply with this line)'; exit 0 }
"PR=https://github.com/$Repo/pull/$pr"
# v19g: wait until the PR head is the commit just pushed; round 5 w02's inline wait read the previous head's RED (stale) and the
# agent then fought a NO_CHANGE publish.
# v26g: NO_CHANGE has no HEAD= line; ($null).Matches piped a $null into ForEach -> "Cannot index into a null array." (round 12 s03)
$want = $out | Select-String 'HEAD=(\w+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1
if ($want) { foreach ($i in 1..12) { if ((gh api "repos/$Repo/pulls/$pr" --jq .head.sha) -eq $want) { break }; Start-Sleep 5 } }
# v18g: wait inline for what is left of an 8-minute budget (Submit itself takes ~1 min), so Submit + first wait is one turn
# instead of two (round 4: wait turns replayed 13.5% of input). v14g's inline wait broke because it used the full 9 min on top.
$left = [int][Math]::Floor(8 - ((Get-Date) - $t0).TotalMinutes)
if ($left -ge 2) { $w = @(& "$tools/Wait-PrChecks.ps1" -Pr $pr -MaxCallMin $left); $w; if ($w -contains 'CHECKS=GREEN') { exit 0 } }  # v31g: no WAIT_CMD after GREEN
"WAIT_CMD=pwsh -NoProfile $tools/Wait-PrChecks.ps1 -Pr $pr -MaxCallMin 9  (only if no CHECKS=GREEN/RED above)"
exit 0
