# v6 general lean loop for any src-only change (phase 3). The agent makes the edits; this script does the rest in ONE call:
#   build UnitTests + IntegrationTests (warnings as errors) -> filtered unit tests -> guarded REST publish (src/ only) -> prints WAIT_CMD.
# It does NOT wait for CI (keeps the call short so the runtime doesn't background it). Then run WAIT_CMD (<=2 min per call).
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [string]$Filter = 'FullyQualifiedName~WorkOrder',
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$tools = Join-Path ([IO.Path]::GetTempPath()) 'agent-scripts'
New-Item -ItemType Directory -Force $tools | Out-Null; Copy-Item "$PSScriptRoot/*.ps1" $tools -Force
$owner = $Repo.Split('/')[0]
$open = gh api "repos/$Repo/pulls?head=${owner}:$Branch&state=open" --jq '.[0].number // empty'
if (-not $open) {
  & "$tools/Invoke-AgentBuildTest.ps1" -Filter $Filter | Where-Object { $_ -match 'FAIL|error' }; if ($LASTEXITCODE) { exit 1 }
  $b = dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
  if ($LASTEXITCODE) { $b | Select-String 'error' | Select-Object -First 10; 'IT_BUILD=FAIL'; exit 1 }
  $title = gh api "repos/$Repo/issues/$Issue" --jq .title
  $out = & "$tools/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
  if ($out -match 'PUBLISH_FAIL') { $out; exit 2 }
  $open = [int](($out | Select-String 'pull/(\d+)').Matches[0].Groups[1].Value)
}
"WAIT_CMD=pwsh -NoProfile $tools/Wait-PrChecks.ps1 -Pr $open -MaxCallMin 2"
