param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = ""
)

# Set database server from pipeline variable if available
if (-not [string]::IsNullOrEmpty($databaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}
else {
	$databaseServer = "(LocalDb)\MSSQLLocalDB"
}

. .\build.ps1

Run-AcceptanceTests -databaseServer $databaseServer -databaseName "ChurchBulletin"