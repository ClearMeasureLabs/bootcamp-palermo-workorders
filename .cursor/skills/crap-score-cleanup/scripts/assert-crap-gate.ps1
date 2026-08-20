#Requires -Version 7.0
<#
.SYNOPSIS
  Exit 1 when crap-production-violations.json reports any in-scope production methods over threshold.

.PARAMETER ViolationsPath
  Path to crap-production-violations.json written by rollup-file-scores.csx.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ViolationsPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ViolationsPath)) {
    Write-Error "CRAP violations file not found: $ViolationsPath"
    exit 2
}

$payload = Get-Content -LiteralPath $ViolationsPath -Raw | ConvertFrom-Json
$count = [int]$payload.violationCount
$threshold = $payload.threshold
if ($count -gt 0) {
    Write-Host "CRAP gate failed: $count production method(s) exceed threshold $threshold"
    if ($payload.methods) {
        foreach ($method in $payload.methods) {
            Write-Host ("  CRAP {0:N1}  CC {1}  cov {2}%  {3}" -f $method.crap, $method.complexity, $method.coverage, $method.fullName)
        }
    }
    exit 1
}

Write-Host "CRAP gate passed: 0 production methods exceed threshold $threshold"
exit 0
