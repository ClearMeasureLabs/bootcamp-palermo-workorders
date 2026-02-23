param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = "(LocalDb)\MSSQLLocalDB",
	
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [bool]$migrateDbWithFlyway = $false,

    [Parameter(Mandatory=$false)]
    [ValidateSet("sqlite", "sqllocaldb", "sqlcontainer")]
    [string]$databaseMode = "sqllocaldb"
	
)

. .\build.ps1

PrivateBuild -mode $databaseMode