param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",

    [Parameter(Mandatory=$false)]
    [string]$databaseName = "ChurchBulletin",

    [Parameter(Mandatory=$false)]
    [switch]$Headful
)

. .\build.ps1

# Set database server from pipeline variable if available
if ([string]::IsNullOrEmpty($databaseServer) -and -not [string]::IsNullOrEmpty($env:DatabaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}

# Set default database server based on platform if not provided
if ([string]::IsNullOrEmpty($databaseServer)) {
    if (Test-IsLinux) {
        $databaseServer = "localhost,1433"
    }
    else {
        $databaseServer = "(LocalDb)\MSSQLLocalDB"
    }
}

if ($Headful) {
    $env:HeadlessTestBrowser = "false"
    Log-Message -Message "Running acceptance tests with headful browser windows." -Type "INFO"
}

Invoke-AcceptanceTests -databaseServer $databaseServer -databaseName $databaseName
