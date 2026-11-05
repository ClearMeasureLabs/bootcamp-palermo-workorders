param()
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
Push-Location $projectRoot
try {
    npm run private-build
    if ($LASTEXITCODE -ne 0) { throw "Native TypeScript private build failed with exit code $LASTEXITCODE" }
}
finally { Pop-Location }
