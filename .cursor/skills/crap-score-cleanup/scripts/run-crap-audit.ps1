#Requires -Version 7.0
<#
.SYNOPSIS
  Collect Cobertura coverage, compute CRAP scores, and roll up per-file metrics.

.PARAMETER Solution
  Path to the .NET solution. Defaults to src/ChurchBulletin.sln.

.PARAMETER OutputDir
  Directory for reports. Defaults to crap-metrics at repo root.

.PARAMETER Threshold
  CRAP threshold for "CRAPpy" methods. When omitted, reads productionThreshold from
  ../crap-gate-threshold.json (single source of truth for the CI/PrivateBuild gate).

.PARAMETER SkipTests
  Skip test run; reuse existing Cobertura files under OutputDir/TestResults.

.PARAMETER AllowPartialCoverage
  Continue when dotnet test exits non-zero. Default is to fail — acceptance tests
  must pass. Use only when diagnosing coverage from a partial run.

.PARAMETER RepoRoot
  Repository root containing src/ChurchBulletin.sln. Defaults to the directory that
  contains the skill (three levels above scripts/). When using a git worktree, pass
  the worktree path explicitly or run the script from that directory.

.PARAMETER Configuration
  dotnet build/test configuration. Default Release.

.PARAMETER FailOnViolations
  Exit 1 when any in-scope production method has CRAP greater than Threshold.
  Test projects and generated code are excluded. Use this for PrivateBuild/CI.

.PARAMETER Quiet
  Suppress progress and report output to the console. Errors still fail the script.
  PrivateBuild uses this so the gate runs without printing the CRAP report.
#>
param(
    [string]$Solution = "src/ChurchBulletin.sln",
    [string]$OutputDir = "crap-metrics",
    [int]$Threshold = 0,
    [switch]$SkipTests,
    [switch]$AllowPartialCoverage,
    [string]$RepoRoot = "",
    [string]$Configuration = "Release",
    [switch]$FailOnViolations,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

function Write-AuditHost {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host $Message
    }
}

function Invoke-QuietExternal {
    param(
        [scriptblock]$Command
    )
    if ($Quiet) {
        & $Command *> $null
    }
    else {
        & $Command
    }
}

function Get-ProductionCrapGateThreshold {
    $configPath = Join-Path $PSScriptRoot ".." "crap-gate-threshold.json"
    if (-not (Test-Path -LiteralPath $configPath)) {
        Write-Error "CRAP gate threshold file not found: $configPath"
    }
    $payload = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    if ($null -eq $payload.productionThreshold) {
        Write-Error "CRAP gate threshold file missing productionThreshold: $configPath"
    }
    $value = 0
    if (-not [int]::TryParse([string]$payload.productionThreshold, [ref]$value) -or $value -le 0) {
        Write-Error "CRAP gate productionThreshold must be a positive integer in $configPath (got '$($payload.productionThreshold)')."
    }
    return $value
}

if (-not $PSBoundParameters.ContainsKey("Threshold")) {
    $Threshold = Get-ProductionCrapGateThreshold
}

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
Set-Location $RepoRoot

$dotnetTools = Join-Path $HOME ".dotnet" "tools"
$env:PATH = "$dotnetTools$([IO.Path]::PathSeparator)$env:PATH"

function Ensure-Tool {
    param([string]$PackageId, [string]$Command, [string]$Version)
    Write-AuditHost "Ensuring $PackageId $Version ..."
    if ($Quiet) {
        & dotnet tool update -g $PackageId --version $Version *> $null
        if ($LASTEXITCODE -ne 0) {
            & dotnet tool install -g $PackageId --version $Version *> $null
        }
    }
    else {
        & dotnet tool update -g $PackageId --version $Version
        if ($LASTEXITCODE -ne 0) {
            & dotnet tool install -g $PackageId --version $Version
        }
    }
    if (-not (Get-Command $Command -ErrorAction SilentlyContinue)) {
        Write-Error "Failed to install or locate $Command ($PackageId $Version)."
    }
}

Ensure-Tool -PackageId "crap4dotnet" -Command "dotnet-crap" -Version "0.1.1"
Ensure-Tool -PackageId "dotnet-script" -Command "dotnet-script" -Version "2.0.0"

$outPath = Join-Path $RepoRoot $OutputDir
New-Item -ItemType Directory -Force -Path $outPath | Out-Null
$testResults = Join-Path $outPath "TestResults"

if (-not $SkipTests) {
    Write-AuditHost "Running build.ps1 test pipeline (Init -> Compile -> Unit -> Integration -> Acceptance) ..."
    Push-Location $RepoRoot
    try {
        . (Join-Path $RepoRoot "build.ps1")

        Resolve-DatabaseEngine
        if ($script:databaseEngine -ne "SQLite") {
            $script:databaseName = Get-ResolvedDatabaseName -explicitName "" -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
        }

        Init
        Compile
        UnitTests
        Setup-DatabaseForBuild
        IntegrationTest
        AcceptanceTests
    }
    finally {
        Pop-Location
    }

    $testResults = Join-Path $RepoRoot "build/test"
}
else {
    $testResults = Join-Path $outPath "TestResults"
    if (-not (Test-Path $testResults)) {
        $testResults = Join-Path $RepoRoot "build/test"
    }
}

$coverageFiles = @(Get-ChildItem -Path $testResults -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue)
if ($coverageFiles.Count -eq 0) {
    Write-Error "No coverage.cobertura.xml found under $testResults. Run without -SkipTests or check test output."
}

Write-AuditHost "Found $($coverageFiles.Count) Cobertura file(s)."

$assertCoreScript = Join-Path $PSScriptRoot "assert-core-cobertura.ps1"
Write-AuditHost "Asserting production ClearMeasure.Bootcamp.Core appears in Cobertura ..."
Invoke-QuietExternal { & $assertCoreScript -CoverageRoot $testResults -RepoRoot $RepoRoot }
if ($LASTEXITCODE -ne 0) {
    Write-Error "Core Cobertura hard-check failed (exit $LASTEXITCODE). Production src/Core must be instrumented via coverlet.runsettings."
}

$flattenScript = Join-Path $PSScriptRoot "flatten-cobertura.csx"
$flattenedCoverage = Join-Path $outPath "coverage.flattened.cobertura.xml"
$flattenArgs = @($flattenScript, "--", $flattenedCoverage) + @($coverageFiles | ForEach-Object { $_.FullName })
Write-AuditHost "Flattening async Cobertura state machines for crap4dotnet ..."
Invoke-QuietExternal { & dotnet-script @flattenArgs }
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $flattenedCoverage)) {
    Write-Error "Failed to flatten Cobertura coverage at $flattenedCoverage"
}

$reportJson = Join-Path $outPath "crap-report.json"
$crapArgs = @(
    "analyze", (Join-Path $RepoRoot $Solution),
    "--threshold", $Threshold,
    "--output", $reportJson,
    "--coverage", $flattenedCoverage
)

Write-AuditHost "Analyzing CRAP scores ..."
Invoke-QuietExternal { & dotnet-crap @crapArgs }
$crapExit = $LASTEXITCODE
# dotnet-crap exits non-zero when CRAPpy methods exist — that is expected.

if (-not (Test-Path $reportJson)) {
    Write-Error "CRAP report not produced at $reportJson"
}

Write-AuditHost "Rolling up file-level scores ..."
$rollupScript = Join-Path $PSScriptRoot "rollup-file-scores.csx"
Invoke-QuietExternal { & dotnet-script $rollupScript -- $reportJson $outPath }
$violationsPath = Join-Path $outPath "crap-production-violations.json"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $violationsPath)) {
    Write-Error "Failed to roll up CRAP scores (dotnet-script exit $LASTEXITCODE). Expected $violationsPath"
}

if (-not $Quiet) {
    Write-Host ""
    Write-Host "=== CRAP audit complete ==="
    Write-Host "  Methods : $reportJson"
    Write-Host "  Files   : $(Join-Path $outPath 'crap-by-file.json')"
    Write-Host "  Summary : $(Join-Path $outPath 'crap-summary.md')"
    Write-Host "  Gate    : $violationsPath"
    if ($crapExit -ne 0) {
        Write-Host "  Note    : dotnet-crap reported CRAPpy methods (exit $crapExit) — review summary (tests/generated may be included)."
    }
}

$assertScript = Join-Path $PSScriptRoot "assert-crap-gate.ps1"
if ($FailOnViolations) {
    $assertArgs = @{
        ViolationsPath = $violationsPath
    }
    if ($Quiet) {
        $assertArgs["Quiet"] = $true
    }
    & $assertScript @assertArgs
    exit $LASTEXITCODE
}
