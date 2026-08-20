<#
.SYNOPSIS
    Runs JetBrains Qodana Community for .NET locally (same gate as CI).

.DESCRIPTION
    Mirrors the GitHub Actions "Qodana (Community .NET)" job:
    jetbrains/qodana-cdnet with src/ChurchBulletin.sln, committed baseline
    qodana.sarif.json, and fail-threshold 0.

    Windows checkouts often use CRLF. The committed baseline SARIF was produced
    on Linux CI (LF). Qodana baseline matching uses content fingerprints, so a
    direct scan of a CRLF working tree marks every finding as NEW. This script
    stages an LF-normalized project tree (git blobs + dirty overlays), then
    points --baseline at qodana.sarif.json relative to that project root — the
    same reference style as CI.

    Prefers the Qodana CLI (`qodana`) when installed; otherwise uses Docker.
    Docker Desktop must be running. Typical duration is about 3–5 minutes after
    the image is cached.

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
    [switch]$PrintProblems,

    [Parameter(Mandatory = $false)]
    [switch]$CommittedOnly
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

$baselineRepoPath = Join-Path $repoRoot $Baseline
if (-not $SkipBaseline -and -not (Test-Path -LiteralPath $baselineRepoPath)) {
    throw "Baseline SARIF not found: $baselineRepoPath (pass -SkipBaseline to scan without a gate baseline)"
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

function ConvertTo-LfText([string]$text) {
    return (($text -replace "`r`n", "`n") -replace "`r", "`n")
}

function Write-Utf8NoBomLf {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )
    $lf = ConvertTo-LfText -text $Text
    $dir = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $lf, $encoding)
}

function Test-LooksBinary([byte[]]$bytes) {
    foreach ($b in $bytes) {
        if ($b -eq 0) { return $true }
    }
    return $false
}

function Copy-FileAsLf {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )
    $bytes = [System.IO.File]::ReadAllBytes($SourcePath)
    $dir = Split-Path -Parent $DestinationPath
    if (-not [string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    if (Test-LooksBinary $bytes) {
        [System.IO.File]::WriteAllBytes($DestinationPath, $bytes)
        return
    }
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    Write-Utf8NoBomLf -Path $DestinationPath -Text $text
}

function Convert-TreeToLf {
    param([Parameter(Mandatory = $true)][string]$Root)
    $count = 0
    Get-ChildItem -LiteralPath $Root -Recurse -File -Force | ForEach-Object {
        $path = $_.FullName
        # Skip obvious binaries / large artifacts
        $ext = $_.Extension.ToLowerInvariant()
        if ($ext -in @('.png', '.jpg', '.jpeg', '.gif', '.webp', '.ico', '.dll', '.exe', '.pdb', '.nupkg', '.snupkg', '.zip', '.mp4', '.woff', '.woff2', '.ttf', '.eot')) {
            return
        }
        $bytes = [System.IO.File]::ReadAllBytes($path)
        if (Test-LooksBinary $bytes) { return }
        $hasCr = $false
        foreach ($b in $bytes) {
            if ($b -eq 13) { $hasCr = $true; break }
        }
        if (-not $hasCr) { return }
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        Write-Utf8NoBomLf -Path $path -Text $text
        $count++
    }
    Write-Host "Normalized $count file(s) to LF (Windows tar/worktree may emit CRLF)."
}

function New-LfProjectStage {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $false)][switch]$IncludeWorktreeChanges
    )

    $stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("qodana-lf-" + [guid]::NewGuid().ToString("n"))
    New-Item -ItemType Directory -Force -Path $stageRoot | Out-Null

    $tarPath = Join-Path $stageRoot "_head.tar"
    Write-Host "Staging LF project tree from git (matches CI line endings)..."
    & git -C $RepoRoot archive --format=tar -o $tarPath HEAD
    if ($LASTEXITCODE -ne 0) {
        throw "git archive failed (exit $LASTEXITCODE)"
    }

    & tar -xf $tarPath -C $stageRoot
    if ($LASTEXITCODE -ne 0) {
        throw "tar extract failed (exit $LASTEXITCODE). Windows 10+ tar.exe is required."
    }
    Remove-Item -LiteralPath $tarPath -Force

    # Windows bsdtar often rewrites LF blobs to CRLF on extract; undo that so
    # Qodana equalIndicator fingerprints match the Linux CI baseline SARIF.
    Convert-TreeToLf -Root $stageRoot

    if ($IncludeWorktreeChanges) {
        $changed = @()
        $porcelain = & git -C $RepoRoot status --porcelain --untracked-files=normal
        foreach ($line in $porcelain) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            $pathPart = $line.Substring(3)
            if ($pathPart -match ' -> ') {
                $pathPart = ($pathPart -split ' -> ', 2)[1]
            }
            $pathPart = $pathPart.Trim('"')
            # Ignore local scan output and agent scratch paths
            if ($pathPart -match '^(qodana-results/|\.cursor/tmp/|\.worktrees/|\.claude/worktrees/)') {
                continue
            }
            $changed += $pathPart
        }

        foreach ($rel in ($changed | Select-Object -Unique)) {
            $src = Join-Path $RepoRoot $rel
            if (-not (Test-Path -LiteralPath $src -PathType Leaf)) {
                $dstDeleted = Join-Path $stageRoot $rel
                if (Test-Path -LiteralPath $dstDeleted) {
                    Remove-Item -LiteralPath $dstDeleted -Force
                }
                continue
            }
            $dst = Join-Path $stageRoot $rel
            Copy-FileAsLf -SourcePath $src -DestinationPath $dst
        }
        if ($changed.Count -gt 0) {
            Write-Host ("Overlaid {0} dirty/untracked path(s) as LF." -f $changed.Count)
        }
    }

    # Always refresh gate files from the repo root working copies (LF-normalized).
    foreach ($required in @("qodana.yaml", $Baseline)) {
        $src = Join-Path $RepoRoot $required
        if (Test-Path -LiteralPath $src) {
            Copy-FileAsLf -SourcePath $src -DestinationPath (Join-Path $stageRoot $required)
        }
    }

    return $stageRoot
}

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
$reportDir = Join-Path $ResultsDir "report"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

$includeWorktree = -not $CommittedOnly
$stageRoot = $null
$started = Get-Date

try {
    $stageRoot = New-LfProjectStage -RepoRoot $repoRoot -IncludeWorktreeChanges:$includeWorktree

    # CI passes a project-relative baseline path. Keep the same contract so the
    # container resolves /data/project/qodana.sarif.json against LF sources.
    $baselineArg = $Baseline
    $baselineInStage = Join-Path $stageRoot $Baseline

    Write-Host "Qodana local scan"
    Write-Host "  Repo:       $repoRoot"
    Write-Host "  Stage (LF): $stageRoot"
    Write-Host "  Image:      $Image"
    Write-Host "  Solution:   $Solution"
    Write-Host "  Baseline:   $(if ($SkipBaseline) { '(skipped)' } else { $baselineArg })"
    Write-Host "  Threshold:  $FailThreshold"
    Write-Host "  Results:    $ResultsDir"
    Write-Host ""

    if (-not $SkipBaseline -and -not (Test-Path -LiteralPath $baselineInStage)) {
        throw "Staged baseline missing: $baselineInStage"
    }

    $cli = Get-QodanaCli
    $exitCode = 1

    if ($null -ne $cli) {
        if (-not (Test-DockerAvailable)) {
            throw "Qodana CLI is installed, but Docker is not available. Start Docker Desktop and retry."
        }

        $scanArgs = @(
            "scan",
            "--image", $Image,
            "--project-dir", $stageRoot,
            "--solution", $Solution,
            "--results-dir", $ResultsDir,
            "--report-dir", $reportDir,
            "--fail-threshold", "$FailThreshold",
            "--disable-update-checks"
        )

        if (-not $SkipBaseline) {
            # Project-relative path (CI style), not a host absolute path.
            $scanArgs += @("--baseline", $baselineArg)
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

        Push-Location $stageRoot
        try {
            & qodana @scanArgs
            $exitCode = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
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

        if (-not $SkipPull) {
            Write-Host "Pulling $Image ..."
            & docker pull $Image
            if ($LASTEXITCODE -ne 0) {
                throw "docker pull failed for $Image"
            }
        }

        $dockerArgs = @(
            "run", "--rm",
            "-v", "${stageRoot}:/data/project",
            "-v", "${ResultsDir}:/data/results",
            $Image,
            "--solution=$Solution",
            "--fail-threshold=$FailThreshold"
        )

        if (-not $SkipBaseline) {
            # Same mount layout CI uses: baseline inside /data/project.
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
        if (-not $SkipBaseline) {
            $raw = Get-Content -LiteralPath $sarifOut -Raw | ConvertFrom-Json
            $states = $raw.runs[0].results | Group-Object baselineState | ForEach-Object { "$($_.Name)=$($_.Count)" }
            Write-Host ("Baseline states: " + ($states -join ", "))
        }
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
}
finally {
    if ($stageRoot -and (Test-Path -LiteralPath $stageRoot)) {
        try {
            Remove-Item -LiteralPath $stageRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Could not remove stage directory: $stageRoot"
        }
    }
}
