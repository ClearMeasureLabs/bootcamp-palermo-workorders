# v6.1 general lean loop (phase 3 rep 2). The agent makes the edits; ONE call then does:
#   FULL unit tests (rep 1: CI failed on unit tests outside the old filter) + IntegrationTests build -> publish (temp worktree on
#   origin/master, src only; the workspace branch never changes) -> prints WAIT_CMD. Rerun after any fix: it pushes a follow-up commit.
# WAIT_CMD waits up to 8 min per call (rep 1's 2-min calls cost ~10 wait turns per CI cycle, each replaying the whole context).
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [string]$Filter = '',
      [string]$Template = '', [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$tools = Join-Path ([IO.Path]::GetTempPath()) 'agent-scripts'
New-Item -ItemType Directory -Force $tools | Out-Null; Copy-Item "$PSScriptRoot/*.ps1" $tools -Force
if ($Template) {  # canned change (rep 3+): apply once
  $tp = Join-Path $PSScriptRoot "templates/$Template.patch"
  git diff --quiet -- src
  if ($LASTEXITCODE -eq 0) { git apply --3way --whitespace=nowarn $tp 2>&1 | Out-Null; if ($LASTEXITCODE) { 'TEMPLATE=FAIL'; exit 1 } }
}
$f = if ($Filter) { $Filter } else { 'FullyQualifiedName!~__none__' }
& "$tools/Invoke-AgentBuildTest.ps1" -Filter $f | Where-Object { $_ -match 'FAIL|error|Failed ' }; if ($LASTEXITCODE) { exit 1 }
$b = dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String 'error' | Select-Object -First 10; 'IT_BUILD=FAIL'; exit 1 }
$title = gh api "repos/$Repo/issues/$Issue" --jq .title
$out = & "$tools/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
if ($out -match 'PUBLISH_FAIL') { $out; exit 2 }
$pr = [int](($out | Select-String 'pull/(\d+)').Matches[0].Groups[1].Value)
"WAIT_CMD=pwsh -NoProfile $tools/Wait-PrChecks.ps1 -Pr $pr -MaxCallMin 8"
