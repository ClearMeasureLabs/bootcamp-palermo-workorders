#Requires -Version 7.0
<#
.SYNOPSIS
  Collect Cobertura coverage, compute CRAP scores, and roll up per-file metrics.

.PARAMETER Solution
  Path to the .NET solution. Defaults to src/ChurchBulletin.sln.

.PARAMETER OutputDir
  Directory for reports. Defaults to crap-metrics at repo root.

.PARAMETER Threshold
  CRAP threshold for "CRAPpy" methods. Default 30.

.PARAMETER SkipTests
  Skip test run; reuse existing Cobertura files under OutputDir/TestResults.

.PARAMETER Configuration
  dotnet build/test configuration. Default Release.
#>
param(
    [string]$Solution = "src/ChurchBulletin.sln",
    [string]$OutputDir = "crap-metrics",
    [int]$Threshold = 30,
    [switch]$SkipTests,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
Set-Location $repoRoot

function Ensure-Tool {
    param([string]$PackageId, [string]$Command, [string]$Version)
    if (Get-Command $Command -ErrorAction SilentlyContinue) { return }
    Write-Host "Installing $PackageId $Version ..."
    dotnet tool install -g $PackageId --version $Version | Out-Null
}

Ensure-Tool -PackageId "crap4dotnet" -Command "dotnet-crap" -Version "0.1.1"
Ensure-Tool -PackageId "dotnet-script" -Command "dotnet-script" -Version "1.6.0"

$outPath = Join-Path $repoRoot $OutputDir
$testResults = Join-Path $outPath "TestResults"
New-Item -ItemType Directory -Force -Path $outPath | Out-Null

if (-not $SkipTests) {
    Write-Host "Running tests with XPlat Code Coverage ..."
    New-Item -ItemType Directory -Force -Path $testResults | Out-Null
    $testArgs = @(
        "test", $Solution,
        "--configuration", $Configuration,
        "--collect:XPlat Code Coverage",
        "--results-directory", $testResults
    )
    & dotnet @testArgs
    $testExit = $LASTEXITCODE
    if ($testExit -ne 0) {
        Write-Warning "dotnet test exited $testExit — continuing with partial coverage if Cobertura files exist."
    }
}

$coverageFiles = @(Get-ChildItem -Path $testResults -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue)
if ($coverageFiles.Count -eq 0) {
    Write-Error "No coverage.cobertura.xml found under $testResults. Run without -SkipTests or check test output."
}

Write-Host "Found $($coverageFiles.Count) Cobertura file(s)."

$reportJson = Join-Path $outPath "crap-report.json"
$crapArgs = @(
    "analyze", (Join-Path $repoRoot $Solution),
    "--threshold", $Threshold,
    "--output", $reportJson
)
foreach ($f in $coverageFiles) {
    $crapArgs += @("--coverage", $f.FullName)
}

Write-Host "Analyzing CRAP scores ..."
& dotnet-crap @crapArgs
$crapExit = $LASTEXITCODE
# dotnet-crap exits non-zero when CRAPpy methods exist — that is expected.

if (-not (Test-Path $reportJson)) {
    Write-Error "CRAP report not produced at $reportJson"
}

Write-Host "Rolling up file-level scores ..."
$rollupScript = Join-Path $PSScriptRoot "rollup-file-scores.csx"
& dotnet-script $rollupScript -- $reportJson $outPath

Write-Host ""
Write-Host "=== CRAP audit complete ==="
Write-Host "  Methods : $reportJson"
Write-Host "  Files   : $(Join-Path $outPath 'crap-by-file.json')"
Write-Host "  Summary : $(Join-Path $outPath 'crap-summary.md')"
if ($crapExit -ne 0) {
    Write-Host "  Note    : dotnet-crap reported CRAPpy methods (exit $crapExit) — review summary."
}
