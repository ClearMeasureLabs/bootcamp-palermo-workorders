<#
.SYNOPSIS
    Scan .NET and npm supply-chain dependencies for known vulnerabilities using OWASP
    Dependency-Check plus the native scanners, and write a consolidated report.

.DESCRIPTION
    Detects which ecosystems are present under -Path, runs the best available OWASP
    Dependency-Check runner (Docker image preferred, installed CLI as fallback), and
    always runs the fast native cross-checks (`dotnet list package --vulnerable` and
    `npm audit`). Merges all findings into <OutDir>/summary.md and leaves the raw
    OWASP DC reports (HTML/JSON/SARIF/etc.) alongside it.

.PARAMETER Path
    Repository root to scan. Default: current directory.

.PARAMETER OutDir
    Directory for reports. Default: <Path>/.security/dependency-scan.

.PARAMETER FailOnCvss
    If set, exit with code 1 when any finding has a CVSS score >= this value.
    Omit for local triage; set (e.g. 7.0) for a CI gate. Range 0-11 (11 disables).

.PARAMETER Format
    Comma-separated OWASP DC report formats: HTML,JSON,SARIF,JUNIT,CSV,XML,ALL. Default: HTML,JSON.

.PARAMETER NvdApiKey
    NVD API key. Defaults to $env:NVD_API_KEY. Without one the first NVD update is slow.

.PARAMETER Runner
    OWASP DC runner: auto | docker | cli. Default auto (Docker if available, else CLI).

.PARAMETER DataDir
    OWASP DC NVD database cache directory. Default: ~/.owasp-dc-data. Persist in CI.

.PARAMETER SkipOwaspDc
    Skip the OWASP Dependency-Check pass and run only the native scanners (fast, no NVD download).

.EXAMPLE
    pwsh scan.ps1 -Path .

.EXAMPLE
    pwsh scan.ps1 -Path . -FailOnCvss 7.0 -Format "HTML,JSON,SARIF,JUNIT"
#>
[CmdletBinding()]
param(
    [string]$Path = ".",
    [string]$OutDir,
    [double]$FailOnCvss = -1,
    [string]$Format = "HTML,JSON",
    [string]$NvdApiKey = $env:NVD_API_KEY,
    [ValidateSet("auto", "docker", "cli")][string]$Runner = "auto",
    [string]$DataDir = (Join-Path $HOME ".owasp-dc-data"),
    [switch]$SkipOwaspDc
)

$ErrorActionPreference = "Stop"
$Path = (Resolve-Path $Path).Path
if (-not $OutDir) { $OutDir = Join-Path $Path ".security/dependency-scan" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "!!  $msg" -ForegroundColor Yellow }
function Have($cmd) { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

# Findings accumulate here: @{ Package; Installed; Fixed; Id; Cvss; Severity; Source }
$findings = [System.Collections.Generic.List[object]]::new()

function Add-Finding($pkg, $installed, $fixed, $id, $cvss, $severity, $source) {
    $findings.Add([pscustomobject]@{
        Package = $pkg; Installed = $installed; Fixed = $fixed; Id = $id
        Cvss = $cvss; Severity = $severity; Source = $source
    })
}

function Get-SeverityRank($sev) {
    switch -Regex ($sev) {
        "critical" { 4 } "high"     { 3 } "moderate|medium" { 2 }
        "low"      { 1 } default    { 0 }
    }
}

# ---------------------------------------------------------------------------
# Detect ecosystems
# ---------------------------------------------------------------------------
Write-Step "Scanning $Path"
$solutions = @(Get-ChildItem -Path $Path -Recurse -Filter *.sln -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[\\/](bin|obj|node_modules)[\\/]" })
$csprojs = @(Get-ChildItem -Path $Path -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[\\/](bin|obj|node_modules)[\\/]" })
$lockfiles = @(Get-ChildItem -Path $Path -Recurse -Filter package-lock.json -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[\\/](node_modules|bin|obj)[\\/]" })
$packageJsons = @(Get-ChildItem -Path $Path -Recurse -Filter package.json -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[\\/](node_modules|bin|obj)[\\/]" })

$hasDotnet = ($solutions.Count + $csprojs.Count) -gt 0
$hasNpm = ($lockfiles.Count + $packageJsons.Count) -gt 0
Write-Host ("    .NET: {0} sln, {1} csproj | npm: {2} lockfile, {3} package.json" -f `
    $solutions.Count, $csprojs.Count, $lockfiles.Count, $packageJsons.Count)

# ---------------------------------------------------------------------------
# Native scanner: .NET
# ---------------------------------------------------------------------------
function Invoke-DotnetScan {
    if (-not (Have "dotnet")) { Write-Warn "dotnet not found; skipping .NET native scan."; return }
    Write-Step ".NET: dotnet list package --vulnerable --include-transitive"
    $targets = if ($solutions.Count) { $solutions } else { $csprojs }
    foreach ($t in $targets) {
        Write-Host "    $($t.Name)"
        # Restore is required for the vulnerability data to resolve; ignore restore noise.
        & dotnet restore $t.FullName --verbosity quiet 2>&1 | Out-Null
        $json = & dotnet list $t.FullName package --vulnerable --include-transitive --format json 2>$null
        $raw = $json -join "`n"
        if (-not $raw.Trim()) { continue }
        try { $doc = $raw | ConvertFrom-Json } catch { Write-Warn "Could not parse dotnet output for $($t.Name)"; continue }
        foreach ($proj in $doc.projects) {
            foreach ($fw in $proj.frameworks) {
                foreach ($p in @($fw.topLevelPackages) + @($fw.transitivePackages)) {
                    if (-not $p.vulnerabilities) { continue }
                    foreach ($v in $p.vulnerabilities) {
                        Add-Finding $p.id $p.resolvedVersion "" $v.advisoryurl "" $v.severity "dotnet"
                    }
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Native scanner: npm
# ---------------------------------------------------------------------------
function Invoke-NpmScan {
    if (-not (Have "npm")) { Write-Warn "npm not found; skipping npm native scan."; return }
    Write-Step "npm audit"
    $dirs = ($lockfiles + $packageJsons | ForEach-Object { $_.DirectoryName } | Sort-Object -Unique)
    foreach ($d in $dirs) {
        Write-Host "    $d"
        Push-Location $d
        try {
            $raw = (& npm audit --json 2>$null) -join "`n"
            if (-not $raw.Trim()) { continue }
            $audit = $raw | ConvertFrom-Json
            if ($audit.vulnerabilities) {
                foreach ($name in $audit.vulnerabilities.PSObject.Properties.Name) {
                    $v = $audit.vulnerabilities.$name
                    $fixed = if ($v.fixAvailable -is [System.Management.Automation.PSCustomObject]) { $v.fixAvailable.version } else { "" }
                    $id = ""
                    if ($v.via) { $viaObj = @($v.via | Where-Object { $_ -is [System.Management.Automation.PSCustomObject] })[0]; if ($viaObj) { $id = $viaObj.url } }
                    Add-Finding $name $v.range $fixed $id "" $v.severity "npm"
                }
            }
        } finally { Pop-Location }
    }
}

# ---------------------------------------------------------------------------
# OWASP Dependency-Check
# ---------------------------------------------------------------------------
function Resolve-Runner {
    if ($Runner -eq "docker") { return "docker" }
    if ($Runner -eq "cli")    { return "cli" }
    if (Have "docker") {
        # Confirm the daemon is reachable, not just the client.
        & docker info *> $null
        if ($LASTEXITCODE -eq 0) { return "docker" }
        Write-Warn "Docker client found but daemon not reachable; trying CLI."
    }
    if ((Have "dependency-check") -or (Have "dependency-check.bat")) { return "cli" }
    return "none"
}

function Test-JavaVersion {
    if (-not (Have "java")) { return $false }
    $v = (& java -version 2>&1) -join " "
    if ($v -match 'version "?(\d+)(?:\.(\d+))?') {
        $major = [int]$Matches[1]
        if ($major -eq 1) { $major = [int]$Matches[2] }  # "1.8" style
        if ($major -lt 11) { Write-Warn "Java $major detected; OWASP DC 12.x needs Java 11+. Use the Docker runner."; return $false }
        return $true
    }
    return $false
}

function Invoke-OwaspDc {
    param([string]$resolved)
    $projectName = Split-Path $Path -Leaf
    $keyArgs = if ($NvdApiKey) { @("--nvdApiKey", $NvdApiKey) } else {
        Write-Warn "No NVD_API_KEY set. First NVD update will be slow (10+ min). Get a free key: https://nvd.nist.gov/developers/request-an-api-key"
        @()
    }
    $formatArgs = @()
    foreach ($f in ($Format -split ",")) { $formatArgs += @("--format", $f.Trim()) }
    $failArgs = if ($FailOnCvss -ge 0) { @("--failOnCVSS", $FailOnCvss) } else { @() }

    if ($resolved -eq "docker") {
        Write-Step "OWASP Dependency-Check (Docker: owasp/dependency-check)"
        $dockerArgs = @(
            "run", "--rm",
            "-v", "${Path}:/src:ro",
            "-v", "${OutDir}:/report",
            "-v", "${DataDir}:/usr/share/dependency-check/data",
            "owasp/dependency-check:latest",
            "--scan", "/src",
            "--project", $projectName,
            "--out", "/report",
            "--enableExperimental",
            "--disableAssembly"    # assembly analyzer needs a bundled dotnet not in the image
        ) + $formatArgs + $keyArgs + $failArgs
        & docker @dockerArgs
        return $LASTEXITCODE
    }
    elseif ($resolved -eq "cli") {
        if (-not (Test-JavaVersion)) { Write-Warn "Skipping OWASP DC CLI run due to Java version. See references/install.md."; return 0 }
        Write-Step "OWASP Dependency-Check (installed CLI)"
        $dc = if (Have "dependency-check") { "dependency-check" } else { "dependency-check.bat" }
        $dcArgs = @(
            "--scan", $Path,
            "--project", $projectName,
            "--out", $OutDir,
            "--data", $DataDir,
            "--enableExperimental"
        ) + $formatArgs + $keyArgs + $failArgs
        & $dc @dcArgs
        return $LASTEXITCODE
    }
    else {
        Write-Warn "No OWASP DC runner available (no Docker daemon, no installed CLI). See references/install.md. Ran native scanners only."
        return 0
    }
}

function Read-OwaspDcJson {
    $jsonReport = Join-Path $OutDir "dependency-check-report.json"
    if (-not (Test-Path $jsonReport)) { return }
    try { $report = Get-Content $jsonReport -Raw | ConvertFrom-Json } catch { Write-Warn "Could not parse OWASP DC JSON report."; return }
    foreach ($dep in $report.dependencies) {
        if (-not $dep.vulnerabilities) { continue }
        $pkg = if ($dep.packages) { $dep.packages[0].id } else { $dep.fileName }
        foreach ($v in $dep.vulnerabilities) {
            $cvss = ""
            if ($v.cvssv3) { $cvss = $v.cvssv3.baseScore } elseif ($v.cvssv2) { $cvss = $v.cvssv2.score }
            Add-Finding $pkg "" "" $v.name $cvss $v.severity "owasp-dc"
        }
    }
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------
$owaspExit = 0
Invoke-DotnetScan
Invoke-NpmScan
if (-not $SkipOwaspDc) {
    $resolved = Resolve-Runner
    $owaspExit = Invoke-OwaspDc -resolved $resolved
    Read-OwaspDcJson
} else {
    Write-Warn "SkipOwaspDc set; ran native scanners only."
}

# ---------------------------------------------------------------------------
# Consolidate -> summary.md
# ---------------------------------------------------------------------------
Write-Step "Writing summary"
$counts = @{ critical = 0; high = 0; moderate = 0; low = 0; other = 0 }
foreach ($f in $findings) {
    switch (Get-SeverityRank $f.Severity) {
        4 { $counts.critical++ } 3 { $counts.high++ } 2 { $counts.moderate++ } 1 { $counts.low++ } default { $counts.other++ }
    }
}
$sorted = $findings | Sort-Object @{ Expression = { Get-SeverityRank $_.Severity }; Descending = $true }, Package

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Dependency scan — $(Split-Path $Path -Leaf)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Critical: $($counts.critical) · High: $($counts.high) · Moderate: $($counts.moderate) · Low: $($counts.low) · Other: $($counts.other)**")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Sources: OWASP Dependency-Check, dotnet list --vulnerable, npm audit")
[void]$sb.AppendLine("")
if ($findings.Count -eq 0) {
    [void]$sb.AppendLine("No known vulnerabilities found. ✅")
} else {
    [void]$sb.AppendLine("| Severity | Package | Installed | Fixed | Advisory/CVE | CVSS | Source |")
    [void]$sb.AppendLine("|----------|---------|-----------|-------|--------------|------|--------|")
    foreach ($f in $sorted) {
        [void]$sb.AppendLine("| $($f.Severity) | $($f.Package) | $($f.Installed) | $($f.Fixed) | $($f.Id) | $($f.Cvss) | $($f.Source) |")
    }
}
$summaryPath = Join-Path $OutDir "summary.md"
$sb.ToString() | Set-Content -Path $summaryPath -Encoding UTF8
$findings | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $OutDir "findings.json") -Encoding UTF8

Write-Host ""
Write-Host "Report: $summaryPath" -ForegroundColor Green
Write-Host ("Findings — Critical: {0}  High: {1}  Moderate: {2}  Low: {3}" -f `
    $counts.critical, $counts.high, $counts.moderate, $counts.low) -ForegroundColor Green

# Exit code: honor OWASP DC's own gate, and also gate on native findings if -FailOnCvss set.
if ($owaspExit -ne 0) { exit $owaspExit }
if ($FailOnCvss -ge 0 -and ($counts.critical + $counts.high) -gt 0) {
    # Native scanners don't emit CVSS; treat High/Critical severity as a gate failure.
    Write-Warn "FailOnCvss set and High/Critical findings present in native scans."
    exit 1
}
exit 0
