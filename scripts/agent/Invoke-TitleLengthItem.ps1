# One-call lean loop for the Title max-length work type: apply, unit-test, SQLite integration test, PR, wait for full CI.
# v2: scripts are copied to a temp dir first, because Publish-AgentPr switches to a branch from master, where scripts/agent doesn't exist.
#     -SkipApply (agent made the edits itself) and -NoWait (agent polls CI itself) exist for ablation runs.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [int]$Max = 1000, [string]$Field = 'Title', [switch]$SkipApply, [switch]$NoWait, [switch]$SkipLocalIT)
$ErrorActionPreference = 'Stop'
$tools = Join-Path ([IO.Path]::GetTempPath()) 'agent-scripts'
New-Item -ItemType Directory -Force $tools | Out-Null
Copy-Item "$PSScriptRoot/*.ps1" $tools -Force
if (-not $SkipApply) {
  if ($Field -eq 'Title') { & "$tools/Set-TitleMaxLength.ps1" -Max $Max | Out-Null }
  else { & "$tools/Set-WorkOrderFieldMaxLength.ps1" -Field $Field -Max $Max | Out-Null }
}
& "$tools/Invoke-AgentBuildTest.ps1" -Filter "FullyQualifiedName~$Field|FullyQualifiedName~WorkOrderManage|FullyQualifiedName~CreateDatedWorkOrders" | Where-Object { $_ -match "FAIL|error" }; if ($LASTEXITCODE) { exit 1 }
if (-not $SkipLocalIT) {
$b = dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE) { $b | Select-String 'error' | Select-Object -First 10; 'IT_BUILD=FAIL'; exit 1 }
$env:DATABASE_ENGINE = 'SQLite'; $env:ConnectionStrings__SqlConnectionString = "Data Source=$([IO.Path]::GetTempPath())it-title.db"
$t = dotnet test src/IntegrationTests/IntegrationTests.csproj -c Release --no-build --filter 'FullyQualifiedName~WorkOrderMappingTests' -v q --nologo 2>&1
$code = $LASTEXITCODE; Remove-Item Env:DATABASE_ENGINE, Env:ConnectionStrings__SqlConnectionString
if ($code) { $t | Select-String 'Failed |Error Message' -Context 0,2 | Select-Object -First 10; 'IT_SQLITE=FAIL'; exit 1 }; 
}
$title = gh issue view $Issue --repo ClearMeasureLabs/bootcamp-palermo-workorders --json title -q .title
$out = & "$tools/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
$pr = [int](($out | Select-String 'pull/(\d+)').Matches[0].Groups[1].Value)
"WAIT_CMD=pwsh -NoProfile $tools/Wait-PrChecks.ps1 -Pr $pr"  # the only line printed before the CI verdict
if ($NoWait) { exit 0 }
& "$tools/Wait-PrChecks.ps1" -Pr $pr; exit $LASTEXITCODE  # exit 3 = still pending: run WAIT_CMD again
