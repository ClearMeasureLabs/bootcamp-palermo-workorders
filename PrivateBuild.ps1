param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = "(LocalDb)\MSSQLLocalDB",
	
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [bool]$migrateDbWithFlyway = $false
	
)

. .\build.ps1

PrivateBuild -databaseServer $databaseServer 