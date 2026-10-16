[CmdletBinding()]
param([switch]$Headful)
$ErrorActionPreference = 'Stop'
if (-not (Get-Command mvn -ErrorAction SilentlyContinue)) { throw 'Maven 3.9+ is required.' }
Push-Location $PSScriptRoot
try {
    if ($Headful) { $env:PLAYWRIGHT_HEADFUL = 'true' }
    & mvn -B -Dtest='*AcceptanceTest' test
    if ($LASTEXITCODE -ne 0) { throw "Acceptance test run failed ($LASTEXITCODE)." }
} finally { Pop-Location }
