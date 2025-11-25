param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = ""
)

. .\build.ps1

# Set database server from pipeline variable if available
if ([string]::IsNullOrEmpty($databaseServer) -and -not [string]::IsNullOrEmpty($env:DatabaseServer)) {
	$databaseServer = $env:DatabaseServer
	Log-Message -Message "Using database server from pipeline variable: $databaseServer" -Type "INFO"
}

Invoke-AcceptanceTests -databaseServer $databaseServer -databaseName "ChurchBulletin"

# Package everything  - a seperate step in the GH action.
#Package-Everything
