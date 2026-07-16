<#
.SYNOPSIS
    Runs StyleCop analysis across the solution and writes a filtered report.

.DESCRIPTION
    Builds ChurchBulletin.sln, extracts every StyleCop (SA####) diagnostic from
    the build output, writes a full report file, and prints a summary grouped by
    rule and by project. StyleCop findings are reported, not build-breaking
    (see src/Directory.Build.props), so this script's exit code reflects whether
    any SA issues were found, not whether the build failed.

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER ReportPath
    Output report file. Default: stylecop-report.txt in the repo root.

.PARAMETER Severity
    Which diagnostics to include: 'warning', 'suggestion', or 'all'. Default: all.

.PARAMETER FailOnIssues
    Exit with code 1 if any SA issue is found (useful for CI gates).

.EXAMPLE
    .\stylecop.ps1

.EXAMPLE
    .\stylecop.ps1 -Severity warning -FailOnIssues
#>
param(
    [string]$Configuration = "Release",
    [string]$ReportPath = "stylecop-report.txt",
    [ValidateSet("warning", "suggestion", "all")]
    [string]$Severity = "all",
    [switch]$FailOnIssues
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $scriptRoot "src/ChurchBulletin.sln"

if (-not (Test-Path $solution)) {
    Write-Error "Solution not found at $solution"
    exit 2
}

# Which message kinds to capture from the build log.
$kinds = switch ($Severity) {
    "warning"    { @("warning") }
    "suggestion" { @("info") }
    default      { @("warning", "info") }
}
$kindPattern = ($kinds -join "|")

Write-Host "Running StyleCop analysis ($Configuration, severity=$Severity)..." -ForegroundColor Cyan
Write-Host "Solution: $solution"

# Force a full re-analysis so warnings are not skipped from incremental build.
# dotnet writes progress to stderr; relax error handling so captured stderr
# (merged via 2>&1) does not terminate the script on PowerShell 7.3+.
$savedEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $global:PSNativeCommandUseErrorActionPreference = $false
}
try {
    $raw = & dotnet build $solution `
        --configuration $Configuration `
        --no-incremental `
        -clp:NoSummary `
        2>&1
    $buildExit = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $savedEap
}

# Match lines like:
#   C:\...\Foo.cs(12,5): warning SA1200: Using directive... [C:\...\Core.csproj]
$saRegex = "(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s+(?<kind>$kindPattern)\s+(?<rule>SA\d{4}):\s+(?<msg>.+?)(\s+\[(?<project>.+?)\])?$"

$issues = foreach ($line in $raw) {
    $text = [string]$line
    $m = [regex]::Match($text, $saRegex)
    if ($m.Success) {
        [pscustomobject]@{
            Rule    = $m.Groups["rule"].Value
            Kind    = $m.Groups["kind"].Value
            File    = $m.Groups["file"].Value.Trim()
            Line    = [int]$m.Groups["line"].Value
            Column  = [int]$m.Groups["col"].Value
            Message = $m.Groups["msg"].Value.Trim()
            Project = if ($m.Groups["project"].Success) { Split-Path $m.Groups["project"].Value -Leaf } else { "" }
        }
    }
}

# De-duplicate (a file analyzed by multiple targets/projects can repeat a line).
$issues = $issues | Sort-Object File, Line, Column, Rule -Unique

$stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$report = New-Object System.Collections.Generic.List[string]
$report.Add("StyleCop analysis report")
$report.Add("Generated : $stamp")
$report.Add("Solution  : $solution")
$report.Add("Config    : $Configuration   Severity: $Severity")
$report.Add("Build exit: $buildExit")
$report.Add("Total SA issues: $($issues.Count)")
$report.Add("")

if ($issues.Count -gt 0) {
    $report.Add("== Summary by rule ==")
    foreach ($g in ($issues | Group-Object Rule | Sort-Object Count -Descending)) {
        $sample = ($g.Group | Select-Object -First 1).Message
        $report.Add(("{0,-8} {1,5}  {2}" -f $g.Name, $g.Count, $sample))
    }
    $report.Add("")

    $report.Add("== Summary by project ==")
    foreach ($g in ($issues | Group-Object Project | Sort-Object Count -Descending)) {
        $name = if ([string]::IsNullOrEmpty($g.Name)) { "(unknown)" } else { $g.Name }
        $report.Add(("{0,5}  {1}" -f $g.Count, $name))
    }
    $report.Add("")

    $report.Add("== All findings ==")
    foreach ($i in ($issues | Sort-Object File, Line, Column)) {
        $report.Add(("{0}({1},{2}): {3} {4}: {5}" -f $i.File, $i.Line, $i.Column, $i.Kind, $i.Rule, $i.Message))
    }
} else {
    $report.Add("No StyleCop issues found.")
}

$reportFull = if ([System.IO.Path]::IsPathRooted($ReportPath)) { $ReportPath } else { Join-Path $scriptRoot $ReportPath }
$report | Set-Content -Path $reportFull -Encoding UTF8

# Console summary.
Write-Host ""
if ($issues.Count -gt 0) {
    Write-Host "StyleCop issues: $($issues.Count)" -ForegroundColor Yellow
    $issues | Group-Object Rule | Sort-Object Count -Descending |
        ForEach-Object { Write-Host ("  {0,-8} {1,5}" -f $_.Name, $_.Count) }
} else {
    Write-Host "No StyleCop issues found." -ForegroundColor Green
}
Write-Host ""
Write-Host "Report written to: $reportFull" -ForegroundColor Cyan

if ($buildExit -ne 0) {
    Write-Warning "Build returned exit code $buildExit (compilation issue, not StyleCop). See output above."
}

if ($FailOnIssues -and $issues.Count -gt 0) {
    exit 1
}
exit 0
