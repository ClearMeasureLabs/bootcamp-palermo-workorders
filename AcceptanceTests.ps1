param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",

    [Parameter(Mandatory=$false)]
    [string]$databaseName = "",

    [Parameter(Mandatory=$false)]
    [switch]$Headful
)

. .\build.ps1

# Set database server from pipeline variable if available
if ([string]::IsNullOrEmpty($databaseServer) -and -not [string]::IsNullOrEmpty($env:DatabaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}

if ($Headful) {
    $env:HeadlessTestBrowser = "false"
    Log-Message -Message "Running acceptance tests with headful browser windows." -Type "INFO"
}

# Pass through only what the user explicitly provided; build.ps1 owns
# DATABASE_ENGINE detection and database-server defaulting.
$buildArgs = @{}
if (-not [string]::IsNullOrEmpty($databaseServer)) {
    $buildArgs["databaseServer"] = $databaseServer
}
if (-not [string]::IsNullOrEmpty($databaseName)) {
    $buildArgs["databaseName"] = $databaseName
}
Invoke-AcceptanceTests @buildArgs
