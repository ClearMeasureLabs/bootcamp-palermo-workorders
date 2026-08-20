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
& $crapAudit -Threshold 13 -SkipTests -FailOnViolations
if ($LASTEXITCODE -ne 0) {
    throw "CRAP gate failed: in-scope production methods exceed threshold 13. See crap-metrics/crap-production-violations.json"
}