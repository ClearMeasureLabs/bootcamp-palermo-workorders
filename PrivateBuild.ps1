param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
	
    [Parameter(Mandatory=$false)]
    [string]$databaseName = ""
)

. .\build.ps1

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
Build @buildArgs

$crapAudit = Join-Path $PSScriptRoot ".cursor/skills/crap-score-cleanup/scripts/run-crap-audit.ps1"
$crapThresholdConfig = Join-Path $PSScriptRoot ".cursor/skills/crap-score-cleanup/crap-gate-threshold.json"
if (-not (Test-Path -LiteralPath $crapThresholdConfig)) {
    throw "CRAP gate threshold file not found: $crapThresholdConfig"
}
$crapThresholdPayload = Get-Content -LiteralPath $crapThresholdConfig -Raw | ConvertFrom-Json
if ($null -eq $crapThresholdPayload.productionThreshold) {
    throw "CRAP gate threshold file missing productionThreshold: $crapThresholdConfig"
}
$crapThreshold = 0
if (-not [int]::TryParse([string]$crapThresholdPayload.productionThreshold, [ref]$crapThreshold) -or $crapThreshold -le 0) {
    throw "CRAP gate productionThreshold must be a positive integer in $crapThresholdConfig (got '$($crapThresholdPayload.productionThreshold)')."
}
# Threshold comes from crap-gate-threshold.json (single source of truth); do not pass -Threshold here.
& $crapAudit -SkipTests -FailOnViolations
if ($LASTEXITCODE -ne 0) {
    throw "CRAP gate failed: in-scope production methods exceed threshold $crapThreshold. See crap-metrics/crap-production-violations.json"
}