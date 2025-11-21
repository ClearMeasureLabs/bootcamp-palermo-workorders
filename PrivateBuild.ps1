param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
	
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [bool]$migrateDbWithFlyway = $false
	
)

. .\build.ps1

# Set default database server based on platform if not provided
if ([string]::IsNullOrEmpty($databaseServer)) {
    if (Test-IsLinux) {
        $databaseServer = "localhost,1433"
    }
    else {
        $databaseServer = "(LocalDb)\MSSQLLocalDB"
    }
}

PrivateBuild -databaseServer $databaseServer 