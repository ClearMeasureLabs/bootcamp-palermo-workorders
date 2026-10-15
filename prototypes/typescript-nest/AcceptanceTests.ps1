param([switch]$Headful)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
Push-Location $projectRoot
try {
    if ($Headful) { $env:HEADFUL = 'true' }
    npm run acceptance
    if ($LASTEXITCODE -ne 0) { throw "Browser acceptance tests failed with exit code $LASTEXITCODE" }
}
finally { Pop-Location }
