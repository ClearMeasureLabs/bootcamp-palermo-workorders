#Requires -Version 7
<#
Runs SemGrep against this codebase and prints a severity-organized summary.

Runs natively on Windows: SemGrep ships win_amd64 wheels (1.170.0 verified),
executed via uvx (uv's tool runner, installed per-user via winget — no admin,
no Python install needed). If semgrep itself is already on PATH (Linux/CI),
it is used directly.

Usage (from repo root):
  pwsh -NoProfile -File .claude/skills/run-semgrep/driver.ps1
  pwsh -NoProfile -File .claude/skills/run-semgrep/driver.ps1 -ScanPath src/UI -Config p/csharp
  pwsh -NoProfile -File .claude/skills/run-semgrep/driver.ps1 -FailOnFindings   # exit 1 if any ERROR
#>
[CmdletBinding()]
param(
    # SemGrep registry rulesets. Registry configs require network access.
    [string[]]$Config = @('p/csharp', 'p/security-audit', 'p/secrets'),
    # File or directory to scan, relative to the current directory.
    [string]$ScanPath = '.',
    # Where the raw SemGrep JSON is written.
    [string]$OutFile = (Join-Path ([System.IO.Path]::GetTempPath()) 'semgrep-results.json'),
    # Exit 1 when any ERROR-severity finding exists (for gating).
    [switch]$FailOnFindings,
    # List every file SemGrep could not fully parse (degraded coverage).
    [switch]$ShowParseErrors,
    # Pinned version for the uvx path (Windows support is official as of 1.170).
    # Ignored when a native semgrep is already on PATH.
    [string]$SemgrepVersion = '1.170.0'
)

$ErrorActionPreference = 'Stop'
$absScanPath = (Resolve-Path $ScanPath).Path
# Scan with a path relative to the current directory when possible, so result
# paths in the summary are repo-relative and clickable.
$relScan = [System.IO.Path]::GetRelativePath((Get-Location).Path, $absScanPath)
$target = if ($relScan.StartsWith('..')) { $absScanPath } else { $relScan }
$configArgs = @($Config | ForEach-Object { '--config', $_ })

function Find-Uvx {
    $uvx = Get-Command uvx -ErrorAction SilentlyContinue
    if ($uvx) { return $uvx.Source }
    # The current shell may predate uv's PATH update - refresh from the registry.
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'User') + ';' + [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $uvx = Get-Command uvx -ErrorAction SilentlyContinue
    if ($uvx) { return $uvx.Source }
    # winget installs uv into its per-user Packages directory.
    $pkg = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\astral-sh.uv*\uvx.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pkg) { return $pkg.FullName }
    return $null
}

function Get-Uvx {
    $found = Find-Uvx
    if ($found) { return $found }
    Write-Host 'uv not found - installing via winget (per-user, no admin)...'
    winget install --id astral-sh.uv -e --accept-source-agreements --accept-package-agreements | Out-Null
    $found = Find-Uvx
    if ($found) { return $found }
    throw 'uvx not found after winget install. Install manually: winget install astral-sh.uv'
}

$native = Get-Command semgrep -ErrorAction SilentlyContinue
if ($native) {
    Write-Host "Using native semgrep: $($native.Source)"
    & semgrep scan @configArgs --metrics=off --quiet --json --output $OutFile $target
    if ($LASTEXITCODE -ne 0) { throw "semgrep exited with code $LASTEXITCODE" }
}
else {
    $uvx = Get-Uvx
    Write-Host "Running semgrep $SemgrepVersion via uvx (rulesets: $($Config -join ', '))... full repo scan takes ~1-2 min."
    & $uvx "semgrep==$SemgrepVersion" scan @configArgs --metrics=off --quiet --json --output $OutFile $target
    if ($LASTEXITCODE -ne 0) { throw "semgrep (uvx) exited with code $LASTEXITCODE" }
}

$report = Get-Content -Raw $OutFile | ConvertFrom-Json
$results = @($report.results)
$parseErrors = @($report.errors | Where-Object { $_.type -and "$($_.type)" -match 'Parsing' })
$scannedCount = if ($report.paths -and $report.paths.scanned) { @($report.paths.scanned).Count } else { '?' }

$sevRank = @{ CRITICAL = 0; ERROR = 1; HIGH = 2; WARNING = 3; MEDIUM = 4; LOW = 5; INFO = 6 }
$groups = $results | Group-Object { $_.extra.severity } |
    Sort-Object { if ($sevRank.ContainsKey($_.Name)) { $sevRank[$_.Name] } else { 99 } }

Write-Host ""
Write-Host "================ SemGrep Summary ================"
Write-Host "Scanned files : $scannedCount"
Write-Host "Rulesets      : $($Config -join ', ')"
Write-Host "Raw JSON      : $OutFile"
$counts = ($groups | ForEach-Object { "$($_.Name): $($_.Count)" }) -join '  '
if (-not $counts) { $counts = 'none' }
Write-Host "Findings      : $($results.Count) total  ($counts)"
Write-Host "Parse errors  : $($parseErrors.Count) files partially parsed (findings in those files may be missed)"
Write-Host "================================================="

foreach ($group in $groups) {
    Write-Host ""
    Write-Host "---- $($group.Name) ($($group.Count)) ----"
    foreach ($f in ($group.Group | Sort-Object check_id, path)) {
        $msg = ($f.extra.message -split "`n")[0]
        if ($msg.Length -gt 220) { $msg = $msg.Substring(0, 220) + '...' }
        $cwe = $f.extra.metadata.cwe
        $cweText = if ($cwe) { " [$(@($cwe)[0])]" } else { '' }
        Write-Host "$($f.path):$($f.start.line)  ($($f.check_id))$cweText"
        Write-Host "    $msg"
    }
}

if ($ShowParseErrors -and $parseErrors.Count -gt 0) {
    Write-Host ""
    Write-Host "---- Partially parsed files ----"
    $parseErrors | ForEach-Object { $_.path } | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
}

if ($FailOnFindings -and ($results | Where-Object { $_.extra.severity -in @('ERROR', 'CRITICAL') })) {
    Write-Host ""
    Write-Host "FailOnFindings: ERROR-severity findings present -> exit 1"
    exit 1
}
