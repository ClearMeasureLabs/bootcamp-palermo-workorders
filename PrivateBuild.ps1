param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = "localhost",
	
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [bool]$migrateDbWithFlyway = $false
	
)

. .\build_linux.ps1

PrivateBuild