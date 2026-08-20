<#
.SYNOPSIS
    Runs JetBrains Qodana Community for .NET locally (same gate as CI).

.DESCRIPTION
    Mirrors the GitHub Actions "Qodana (Community .NET)" job:
    jetbrains/qodana-cdnet with src/ChurchBulletin.sln, committed baseline
    qodana.sarif.json, and fail-threshold 0.

    Prefers the Qodana CLI (`qodana`) when installed; otherwise uses Docker.
    qodana-cdnet always analyzes inside a container — Docker Desktop (or
    equivalent) must be running. Typical duration is about 3–5 minutes after
    the image is cached; first pull is longer.

.EXAMPLE
    pwsh -NoProfile -File ./Qodana.ps1

.EXAMPLE
    pwsh -NoProfile -File ./Qodana.ps1 -SkipBaseline

.EXAMPLE
    pwsh -NoProfile -File ./Qodana.ps1 -ShowReport -ClearCache
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ResultsDir = "",

    [Parameter(Mandatory = $false)]
    [string]$Image = "jetbrains/qodana-cdnet:latest",

    [Parameter(Mandatory = $false)]
    [string]$Solution = "src/ChurchBulletin.sln",

    [Parameter(Mandatory = $false)]
    [string]$Baseline = "qodana.sarif.json",

    [Parameter(Mandatory = $false)]
    [int]$FailThreshold = 0,

    [Parameter(Mandatory = $false)]
    [switch]$SkipBaseline,

    [Parameter(Mandatory = $false)]
    [switch]$ClearCache,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPull,

    [Parameter(Mandatory = $false)]
    [switch]$ShowReport,

    [Parameter(Mandatory = $false)]
    [switch]$PrintProblems
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($ResultsDir)) {
    $ResultsDir = Join-Path $repoRoot "qodana-results"
}

$configPath = Join-Path $repoRoot "qodana.yaml"
if (-not (Test-Path -LiteralPath $configPath)) {
    throw "qodana.yaml not found at repo root: $configPath"
}

$solutionPath = Join-Path $repoRoot ($Solution -replace '/', [IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution not found: $solutionPath"
}

$baselinePath = $null
if (-not $SkipBaseline) {
    $baselinePath = Join-Path $repoRoot $Baseline
    if (-not (Test-Path -LiteralPath $baselinePath)) {
        throw "Baseline SARIF not found: $baselinePath (pass -SkipBaseline to scan without a gate baseline)"
    }
}

function Test-DockerAvailable {
    try {
        & docker info 1>$null 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
}

function Get-QodanaCli {
    return Get-Command qodana -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
$reportDir = Join-Path $ResultsDir "report"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

$started = Get-Date
Write-Host "Qodana local scan"
Write-Host "  Repo:       $repoRoot"
Write-Host "  Image:      $Image"
Write-Host "  Solution:   $Solution"
Write-Host "  Baseline:   $(if ($SkipBaseline) { '(skipped)' } else { $Baseline })"
Write-Host "  Threshold:  $FailThreshold"
Write-Host "  Results:    $ResultsDir"
Write-Host ""

$cli = Get-QodanaCli
$exitCode = 1

if ($null -ne $cli) {
    if (-not (Test-DockerAvailable)) {
        throw "Qodana CLI is installed, but Docker is not available. Start Docker Desktop and retry."
    }

    $scanArgs = @(
        "scan",
        "--image", $Image,
        "--project-dir", $repoRoot,
        "--solution", $Solution,
        "--results-dir", $ResultsDir,
        "--report-dir", $reportDir,
        "--fail-threshold", "$FailThreshold",
        "--disable-update-checks"
    )

    if (-not $SkipBaseline) {
        $scanArgs += @("--baseline", $baselinePath)
    }
    if ($ClearCache) {
        $scanArgs += "--clear-cache"
    }
    if ($SkipPull) {
        $scanArgs += "--skip-pull"
    }
    if ($ShowReport) {
        $scanArgs += "--show-report"
    }
    if ($PrintProblems) {
        $scanArgs += "--print-problems"
    }

    Write-Host "Using Qodana CLI: $($cli.Source)"
    Write-Host ("qodana " + ($scanArgs -join " "))
    Write-Host ""

    & qodana @scanArgs
    $exitCode = $LASTEXITCODE
}
else {
    if (-not (Test-DockerAvailable)) {
        throw @"
Neither Qodana CLI nor Docker is available.
Install one of:
  winget install JetBrains.QodanaCLI
  Docker Desktop (required for qodana-cdnet either way)
"@
    }

    # Docker Desktop on Windows accepts the host path as the volume source.
    if (-not $SkipPull) {
        Write-Host "Pulling $Image ..."
        & docker pull $Image
        if ($LASTEXITCODE -ne 0) {
            throw "docker pull failed for $Image"
        }
    }

    $dockerArgs = @(
        "run", "--rm",
        "-v", "${repoRoot}:/data/project",
        "-v", "${ResultsDir}:/data/results",
        $Image,
        "--solution=$Solution",
        "--fail-threshold=$FailThreshold"
    )

    if (-not $SkipBaseline) {
        # Baseline is inside the project mount.
        $dockerArgs += "--baseline=/data/project/$Baseline"
    }

    Write-Host "Qodana CLI not found; using Docker image directly."
    Write-Host ("docker " + ($dockerArgs -join " "))
    Write-Host ""

    & docker @dockerArgs
    $exitCode = $LASTEXITCODE
}

$elapsed = (Get-Date) - $started
Write-Host ""
Write-Host ("Finished in {0:mm\:ss} (exit code {1})" -f $elapsed, $exitCode)

$sarifOut = Join-Path $ResultsDir "qodana.sarif.json"
if (Test-Path -LiteralPath $sarifOut) {
    Write-Host "SARIF:  $sarifOut"
}
$htmlIndex = Join-Path $reportDir "index.html"
if (Test-Path -LiteralPath $htmlIndex) {
    Write-Host "Report: $htmlIndex"
}

if ($exitCode -ne 0) {
    throw "Qodana failed with exit code $exitCode (quality gate or scan error)."
}

Write-Host "Qodana passed."
exit 0
