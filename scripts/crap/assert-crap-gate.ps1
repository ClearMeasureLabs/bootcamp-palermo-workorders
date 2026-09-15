#Requires -Version 7.0
<#
.SYNOPSIS
  Exit 1 when crap-production-violations.json reports any in-scope production methods over threshold.

.PARAMETER ViolationsPath
  Path to crap-production-violations.json written by rollup-file-scores.csx.

.PARAMETER Quiet
  Suppress gate result output to the console. Exit codes are unchanged.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ViolationsPath,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ViolationsPath)) {
    Write-Error "CRAP violations file not found: $ViolationsPath"
    exit 2
}

try {
    $payload = Get-Content -LiteralPath $ViolationsPath -Raw | ConvertFrom-Json
}
catch {
    [Console]::Error.WriteLine("CRAP violations file is not valid JSON: $ViolationsPath ($($_.Exception.Message))")
    exit 2
}

# Fail closed: the gate only passes on a well-formed, non-negative integer violationCount
# that agrees with the number of listed methods.
$count = 0
if ($null -eq $payload -or $null -eq $payload.violationCount -or
    -not [int]::TryParse([string]$payload.violationCount, [ref]$count) -or $count -lt 0) {
    [Console]::Error.WriteLine("CRAP violations file has a missing, non-integer, or negative violationCount: $ViolationsPath")
    exit 2
}
$listedMethods = @($payload.methods).Count
if ($count -ne $listedMethods) {
    [Console]::Error.WriteLine("CRAP violations file is inconsistent: violationCount=$count but $listedMethods method(s) listed: $ViolationsPath")
    exit 2
}
$threshold = $payload.threshold
if ($count -gt 0) {
    if (-not $Quiet) {
        Write-Host "CRAP gate failed: $count production method(s) exceed threshold $threshold"
        if ($payload.methods) {
            foreach ($method in $payload.methods) {
                Write-Host ("  CRAP {0:N1}  CC {1}  cov {2}%  {3}" -f $method.crap, $method.complexity, $method.coverage, $method.fullName)
            }
        }
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host "CRAP gate passed: 0 production methods exceed threshold $threshold"
}
exit 0
