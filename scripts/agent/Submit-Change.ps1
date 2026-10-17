# v10g: WAIT_CMD 5-min calls (rep 1: 8-min calls often came back empty). v9g (phase 4): general submit for ANY change, no template. Verify (full unit suite + IntegrationTests build, quiet,
# warnings as errors) -> publish the workspace src/ diff from a temp worktree on origin/master (workspace branch never
# changes; rerun pushes a follow-up commit) -> prints WAIT_CMD (wait script auto-reruns flaky acceptance/integration jobs
# once, v9 records a rerun only when GitHub accepted it). -VerifyOnly = the single local verify command, no publish.
param([int]$Issue, [string]$Branch, [switch]$VerifyOnly, [string]$Filter = '',
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$tools = Join-Path ([IO.Path]::GetTempPath()) 'agent-scripts'
New-Item -ItemType Directory -Force $tools | Out-Null; Copy-Item "$PSScriptRoot/*.ps1" $tools -Force
$f = if ($Filter) { $Filter } else { 'FullyQualifiedName!~__none__' }
& "$tools/Invoke-AgentBuildTest.ps1" -Filter $f | Where-Object { $_ -match 'FAIL|error|Failed ' }; if ($LASTEXITCODE) { exit 1 }
$b = dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String 'error' | Select-Object -First 10; 'IT_BUILD=FAIL'; exit 1 }
if ($VerifyOnly) { 'VERIFY=OK'; exit 0 }
if (-not $Issue -or -not $Branch) { 'need -Issue and -Branch to publish'; exit 1 }
$title = gh api "repos/$Repo/issues/$Issue" --jq .title
$out = & "$tools/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
if ($out -match 'PUBLISH_FAIL') { $out; exit 2 }
$out | Where-Object { $_ -match 'NO_CHANGE' }
$pr = [int](($out | Select-String 'pull/(\d+)').Matches[0].Groups[1].Value)
"WAIT_CMD=pwsh -NoProfile $tools/Wait-PrChecks.ps1 -Pr $pr -MaxCallMin 5"
