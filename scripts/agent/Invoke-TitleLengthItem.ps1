# One-call lean loop for the Title max-length work type: apply, unit-test, SQLite integration test, PR, wait for full CI.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Branch, [int]$Max = 1000)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Set-TitleMaxLength.ps1" -Max $Max | Where-Object { $_ -notmatch '^SAME' }
& "$PSScriptRoot/Invoke-AgentBuildTest.ps1" -Filter "FullyQualifiedName~Title|FullyQualifiedName~WorkOrderManage|FullyQualifiedName~CreateDatedWorkOrders"; if ($LASTEXITCODE) { exit 1 }
dotnet build src/IntegrationTests/IntegrationTests.csproj -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1 | Select-String 'error' | Select-Object -First 10
if ($LASTEXITCODE) { 'IT_BUILD=FAIL'; exit 1 }
$env:DATABASE_ENGINE = 'SQLite'; $env:ConnectionStrings__SqlConnectionString = "Data Source=$([IO.Path]::GetTempPath())it-title.db"
dotnet test src/IntegrationTests/IntegrationTests.csproj -c Release --no-build --filter 'FullyQualifiedName~WorkOrderMappingTests' -v q --nologo 2>&1 | Select-String 'Passed!|Failed!|Failed ' | Select-Object -First 10
if ($LASTEXITCODE) { 'IT_SQLITE=FAIL'; exit 1 }; 'IT_SQLITE=OK'
Remove-Item Env:DATABASE_ENGINE, Env:ConnectionStrings__SqlConnectionString
$title = gh issue view $Issue --repo ClearMeasureLabs/bootcamp-palermo-workorders --json title -q .title
$out = & "$PSScriptRoot/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title; $out
$pr = [int](($out | Select-String 'pull/(\d+)').Matches[0].Groups[1].Value)
& "$PSScriptRoot/Wait-PrChecks.ps1" -Pr $pr; exit $LASTEXITCODE
