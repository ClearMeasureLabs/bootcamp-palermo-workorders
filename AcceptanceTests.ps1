param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = ""
)

# Set database server from pipeline variable if available
if ([string]::IsNullOrEmpty($databaseServer) -and -not [string]::IsNullOrEmpty($env:DatabaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}

. .\build.ps1

Run-AcceptanceTests -databaseServer $databaseServer -databaseName "ChurchBulletin"