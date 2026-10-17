# Quiet build of UnitTests (pulls in Core) with warnings as errors, then filtered tests. Prints only a summary or errors.
param([Parameter(Mandatory)][string]$Filter, [string]$Project = 'src/UnitTests/UnitTests.csproj')
$ErrorActionPreference = 'Stop'
$b = dotnet build $Project -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE -ne 0) { $b | Select-String -Pattern 'error' | Select-Object -First 20; 'BUILD=FAIL'; exit 1 }
'BUILD=OK'
$t = dotnet test $Project -c Release --no-build --filter $Filter -v q --nologo 2>&1
$t | Select-String -Pattern 'Passed!|Failed!|Failed |error' | Select-Object -First 20
if ($LASTEXITCODE -ne 0) { 'TEST=FAIL'; exit 1 } else { 'TEST=OK' }
