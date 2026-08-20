#Requires -Version 7.0
<#
.SYNOPSIS
  Fail when Cobertura coverage lacks production ClearMeasure.Bootcamp.Core (src/Core) line hits.

.PARAMETER CoverageRoot
  Directory to search recursively for coverage.cobertura.xml (default: build/test under repo root).

.PARAMETER RepoRoot
  Repository root. Defaults to current directory when it contains src/ChurchBulletin.sln.
#>
param(
    [string]$CoverageRoot = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $cwdRoot = (Get-Location).Path
    if (Test-Path (Join-Path $cwdRoot "src/ChurchBulletin.sln")) {
        $RepoRoot = $cwdRoot
    }
    else {
        $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
    }
}
else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

if ([string]::IsNullOrWhiteSpace($CoverageRoot)) {
    $CoverageRoot = Join-Path $RepoRoot "build/test"
}

if (-not (Test-Path -LiteralPath $CoverageRoot)) {
    Write-Error "Coverage root not found: $CoverageRoot. Run unit/integration tests with Coverlet first."
    exit 2
}

$coverageFiles = @(Get-ChildItem -Path $CoverageRoot -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue)
if ($coverageFiles.Count -eq 0) {
    Write-Host "Core Cobertura hard-check failed: no coverage.cobertura.xml under $CoverageRoot"
    exit 1
}

function Test-HasProductionCoreHits {
    param([string]$XmlPath)

    [xml]$doc = Get-Content -LiteralPath $XmlPath -Raw
    $classes = @($doc.SelectNodes("//class"))
    foreach ($class in $classes) {
        $filename = [string]$class.filename
        if ([string]::IsNullOrWhiteSpace($filename)) {
            continue
        }

        $normalized = $filename.Replace('\', '/').ToLowerInvariant()
        if ($normalized.Contains('/unittests/') -or
            $normalized.Contains('/integrationtests/') -or
            $normalized.Contains('/acceptancetests/')) {
            continue
        }

        $isCore = $normalized.Contains('/src/core/') -or $normalized.StartsWith('src/core/')
        if (-not $isCore) {
            continue
        }

        foreach ($line in @($class.SelectNodes(".//line"))) {
            $hits = 0
            [void][int]::TryParse([string]$line.hits, [ref]$hits)
            if ($hits -gt 0) {
                return $true
            }
        }
    }

    return $false
}

$found = $false
$matchedFile = $null
foreach ($file in $coverageFiles) {
    if (Test-HasProductionCoreHits -XmlPath $file.FullName) {
        $found = $true
        $matchedFile = $file.FullName
        break
    }
}

if (-not $found) {
    Write-Host "Core Cobertura hard-check failed: no production src/Core filename with line hits > 0 in $($coverageFiles.Count) Cobertura file(s) under $CoverageRoot."
    Write-Host "Ensure coverlet.runsettings Include=[ClearMeasure.Bootcamp.*]* is passed via --settings on Unit/Integration tests."
    exit 1
}

Write-Host "Core Cobertura hard-check passed: production Core hits found in $matchedFile"
exit 0
