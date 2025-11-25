param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = ""
)

# Set database server from pipeline variable if available
if ([string]::IsNullOrEmpty($databaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}
else {
	$databaseServer = $databaseServer
}

. .\build.ps1

Invoke-AcceptanceTests -databaseServer $databaseServer -databaseName "ChurchBulletin"