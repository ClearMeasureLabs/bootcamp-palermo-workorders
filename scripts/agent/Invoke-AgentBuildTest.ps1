# Quiet build of UnitTests (pulls in Core) with warnings as errors, then filtered tests. Prints only a summary or errors.
param([Parameter(Mandatory)][string]$Filter, [string]$Project = 'src/UnitTests/UnitTests.csproj')
$ErrorActionPreference = 'Stop'
$b = dotnet build $Project -c Release -warnaserror -p:NuGetAudit=false -v q -nologo -clp:NoSummary 2>&1
if ($LASTEXITCODE -ne 0) { $b | Select-String -Pattern 'error' | Select-Object -First 20; 'BUILD=FAIL'; exit 1 }
'BUILD=OK'
# v22g: '-v q' alone hides failure details (round 8 z04: TEST=FAIL with nothing else, 3 blind resubmits + 2.4M tokens of digging)
$t = dotnet test $Project -c Release --no-build --filter $Filter -v q --nologo --logger 'console;verbosity=minimal' 2>&1
$code = $LASTEXITCODE
# v19g: on failure include the assertion text after each failed test (round 5 w05: names only, agent ran dotnet test by hand)
if ($code -ne 0) {
  $keep = $false; $stk = $false; $n = 0
  $lines = foreach ($l in $t) { $x = "$l"
    if ($x -match '^\s*Failed \S') { $keep = $true; $stk = $false; $n = 0 }
    elseif ($x -match '^\s*Stack Trace:') { $stk = $true; continue }
    if ($stk) { if ($x -match ' in .*[/\\]src[/\\].*:line \d+') { $x.Trim() }; if ($x -notmatch '^\s+at ') { $stk = $false; $keep = $false }; continue }
    if ($keep -and $n -lt 8) { $n++; $x.TrimEnd() } }
  $lines | Select-Object -First 40
  if (-not $lines) { $t | Select-Object -Last 15 }
  'TEST=FAIL'; exit 1 }
$t | Select-String -Pattern 'Passed!' ; 'TEST=OK'
