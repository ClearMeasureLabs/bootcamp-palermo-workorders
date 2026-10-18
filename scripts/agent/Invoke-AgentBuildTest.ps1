# Quiet build of UnitTests (pulls in Core) with warnings as errors, then filtered tests. Prints only a summary or errors.
param([Parameter(Mandatory)][string]$Filter, [string]$Project = 'src/UnitTests/UnitTests.csproj')
$ErrorActionPreference = 'Stop'
$b = dotnet build $Project -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE -ne 0) { $b | Select-String -Pattern 'error' | Select-Object -First 20; 'BUILD=FAIL'; exit 1 }
'BUILD=OK'
$t = dotnet test $Project -c Release --no-build --filter $Filter -v q --nologo 2>&1
$code = $LASTEXITCODE
# v19g: on failure include the assertion text after each failed test (round 5 w05: names only, agent ran dotnet test by hand)
if ($code -ne 0) { $t | Select-String -Pattern '^\s*Failed |Error Message|Shouldly|Expected|Exception' -Context 0,1 | Select-Object -First 12 | ForEach-Object { $_.ToString().Trim() }; 'TEST=FAIL'; exit 1 }
$t | Select-String -Pattern 'Passed!' ; 'TEST=OK'
