param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
	
    [Parameter(Mandatory=$false)]
    [string]$databaseName = "",

    [Parameter(Mandatory=$false)]
    [ValidateSet("", "LocalDB", "SQL-Container", "SQLite", "SqlServer")]
    [string]$databaseEngine = ""
)

if (-not [string]::IsNullOrEmpty($databaseEngine)) {
    $env:DATABASE_ENGINE = $databaseEngine
}

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