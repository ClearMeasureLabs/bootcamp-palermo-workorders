#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs Roslynator `analyze` against a .NET solution and prints a parsed summary.

.DESCRIPTION
    Deterministic wrapper for the roslynator-analysis skill. Handles:
      * preflight: verifies the Roslynator CLI is installed
      * self-healing: detects the SDK-10 MSBuild `FileUtilities` TypeLoadException and
        neutralizes the bundled Microsoft.Build.Framework.dll, then retries once
        (see resources/TROUBLESHOOTING.md for the underlying cause)
      * running `analyze` to a timestamped XML report under the OS temp directory
      * parsing the report: severity breakdown, compiler (CS*) vs analyzer split, top rules

    Non-interactive by design. If -Solution is omitted it uses the sole discovered
    solution, or exits 3 listing candidates so the caller can pick one and re-invoke.

.PARAMETER Solution
    Path to a .sln/.slnx. If omitted, discovers solutions under the current directory.

.PARAMETER SeverityLevel
    Minimum severity to report: hidden | info | warning | error. Default: info.

.PARAMETER Output
    Directory for the XML report. Default: <temp>/roslynator-analysis. Each run writes a
    timestamped file.

.PARAMETER TopRules
    How many analyzer rules to list in the summary. Default: 20.

.OUTPUTS
    Human-readable summary on stdout.

.NOTES
    Exit codes: 0 success (report produced, diagnostics allowed) · 2 tool not installed ·
    3 solution resolution failed · 4 analysis failed for another reason.
#>
[CmdletBinding()]
param(
    [string]$Solution,
    [ValidateSet('hidden', 'info', 'warning', 'error')]
    [string]$SeverityLevel = 'info',
    [string]$Output,
    [int]$TopRules = 20
)

$ErrorActionPreference = 'Stop'

function Write-Section($text) { Write-Host ''; Write-Host "=== $text ===" }
function Fail($msg, $code) { [Console]::Error.WriteLine($msg); exit $code }

# --- 1. Preflight: Roslynator installed? ---------------------------------------
$version = $null
try { $version = (& roslynator --version) 2>$null } catch { }
if (-not $version) {
    Fail "Roslynator CLI not found. Install with: dotnet tool install -g roslynator.dotnet.cli" 2
}
Write-Host "Roslynator $version"

# --- 2. Resolve the solution ---------------------------------------------------
if (-not $Solution) {
    $found = @(
        Get-ChildItem -Path (Get-Location) -Recurse -File -Filter '*.sln' -ErrorAction SilentlyContinue
        Get-ChildItem -Path (Get-Location) -Recurse -File -Filter '*.slnx' -ErrorAction SilentlyContinue
    )
    if (@($found).Count -eq 0) {
        Fail "No .sln/.slnx found under $(Get-Location). Pass -Solution explicitly." 3
    }
    elseif (@($found).Count -eq 1) {
        $Solution = $found[0].FullName
    }
    else {
        Write-Host "Multiple solutions found - pass one via -Solution:"
        $found | ForEach-Object { Write-Host "  $($_.FullName)" }
        exit 3
    }
}
if (-not (Test-Path $Solution)) {
    Fail "Solution not found: $Solution" 3
}
$Solution = (Resolve-Path $Solution).Path
Write-Host "Solution: $Solution"

# --- 3. Output path (temp-directory default, timestamped filename) -------------
if (-not $Output) {
    $Output = Join-Path ([System.IO.Path]::GetTempPath()) 'roslynator-analysis'
}
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$report = Join-Path $Output ("analysis-{0}.xml" -f (Get-Date -Format 'yyyyMMdd-HHmmssfff'))

# --- Helper: neutralize the bundled MSBuild framework DLL (SDK-10 workaround) ---
function Repair-BundledMSBuild {
    $roots = @(
        (Join-Path $HOME '.dotnet/tools/.store'),
        (Join-Path $env:USERPROFILE '.dotnet/tools/.store')
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique
    $renamed = 0
    foreach ($root in $roots) {
        Get-ChildItem -Path $root -Recurse -Filter 'Microsoft.Build.Framework.dll' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match 'roslynator\.dotnet\.cli' } |
            ForEach-Object {
                try {
                    Rename-Item $_.FullName "$($_.FullName).bak" -Force
                    Write-Host "  neutralized $($_.FullName)"
                    $renamed++
                }
                catch {
                    Write-Host "  failed to neutralize $($_.FullName): $($_.Exception.Message)"
                }
            }
    }
    return $renamed
}

# --- 4. Run analysis (self-healing retry on the SDK-10 MSBuild conflict) --------
function Invoke-Analyze {
    $out = & roslynator analyze $Solution --output $report --severity-level $SeverityLevel --verbosity minimal 2>&1
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($out -join "`n") }
}

Write-Section "Analyzing"
$result = Invoke-Analyze
# roslynator: exit 0 = clean, 1 = diagnostics found (both success). >=2 = failure.
if ($result.ExitCode -ge 2 -and $result.Output -match 'FileUtilities|Microsoft\.Build\.Framework') {
    Write-Host "Detected SDK/MSBuild assembly conflict - applying workaround (see resources/TROUBLESHOOTING.md):"
    if ((Repair-BundledMSBuild) -gt 0) {
        Write-Host "Retrying analysis..."
        $result = Invoke-Analyze
    }
}

if (-not (Test-Path $report) -or $result.ExitCode -ge 2) {
    Write-Host $result.Output
    Fail "Roslynator analysis failed (exit $($result.ExitCode)). See resources/TROUBLESHOOTING.md." 4
}

# --- 5. Parse and summarize ----------------------------------------------------
[xml]$xml = Get-Content -Raw $report
$summary = @($xml.Roslynator.CodeAnalysis.Summary.Diagnostic)
$total = ($summary | Measure-Object -Property Count -Sum).Sum
if (-not $total) { $total = 0 }

Write-Section "Summary"
Write-Host ("Total diagnostics: {0}  (distinct rules: {1})" -f $total, $summary.Count)

if ($total -gt 0) {
    # Detail nodes carry Severity as a child element: <Diagnostic Id="..."><Severity>Info</Severity>...
    $detail = @($xml.SelectNodes('//Projects//Diagnostic'))
    if ($detail.Count -gt 0) {
        Write-Host ''
        Write-Host "By severity:"
        $detail | Group-Object { $_.Severity } | Sort-Object { $_.Count } -Descending |
            ForEach-Object { Write-Host ("  {0,-9} {1}" -f $_.Name, $_.Count) }
    }

    $compiler = @($summary | Where-Object { $_.Id -like 'CS*' })
    $analyzer = @($summary | Where-Object { $_.Id -notlike 'CS*' })
    $compilerTotal = ($compiler | Measure-Object Count -Sum).Sum
    $analyzerTotal = ($analyzer | Measure-Object Count -Sum).Sum

    Write-Host ''
    Write-Host ("Compiler (CS*): {0} across {1} rules  <- usually workspace-resolution noise, not defects." -f ([int]$compilerTotal), $compiler.Count)
    Write-Host '   If a normal build is green, ignore these. `dotnet restore` first reduces them.'
    Write-Host ("Analyzer:       {0} across {1} rules  <- the actionable findings." -f ([int]$analyzerTotal), $analyzer.Count)

    if ($analyzer.Count -gt 0) {
        Write-Section ("Top analyzer rules (max {0})" -f $TopRules)
        $analyzer | Sort-Object { [int]$_.Count } -Descending | Select-Object -First $TopRules |
            ForEach-Object { Write-Host ("  {0,-11} {1,4}  {2}" -f $_.Id, $_.Count, $_.Title) }
    }
}

Write-Section "Report"
$uri = ([System.Uri]$report).AbsoluteUri
Write-Host "  $report"
Write-Host "  $uri"
exit 0
