param (
    [Parameter(Mandatory=$false)]
    [string]$databaseServer = "",
	
    [Parameter(Mandatory=$false)]
    [string]$databaseName = ""
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

# Pass database name to Invoke-PrivateBuild if provided
if ([string]::IsNullOrEmpty($databaseName)) {
    Invoke-PrivateBuild -databaseServer $databaseServer
}
else {
    Invoke-PrivateBuild -databaseServer $databaseServer -databaseName $databaseName
}