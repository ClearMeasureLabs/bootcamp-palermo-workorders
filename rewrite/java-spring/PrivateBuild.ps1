[CmdletBinding()]
param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (-not (Get-Command java -ErrorAction SilentlyContinue)) { throw 'JDK 17+ is required.' }
if (-not (Get-Command mvn -ErrorAction SilentlyContinue)) { throw 'Maven 3.9+ is required.' }
$version = (& java -version 2>&1 | Select-Object -First 1).ToString()
Write-Host "Java runtime: $version"
Push-Location $projectRoot
try {
    if ($SkipTests) { & mvn -B -DskipTests package } else { & mvn -B clean verify }
    if ($LASTEXITCODE -ne 0) { throw "Maven private build failed ($LASTEXITCODE)." }
    Write-Host 'Java private build succeeded; JPA initialized the local H2 schema during tests.'
} finally { Pop-Location }
