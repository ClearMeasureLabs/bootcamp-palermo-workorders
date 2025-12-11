param (
    [Parameter(Mandatory = $false)]
    [string]$databaseServer = "",

    [Parameter(Mandatory = $false)]
    [string]$databaseName = ""
)

. .\build.ps1

if ( [string]::IsNullOrEmpty($databaseServer))
{
    # Set default database server based on platform if not provided
    if (Test-IsLinux)
    {
        $databaseServer = "localhost,1433"
    }
    else
    {
        $databaseServer = "(LocalDb)\MSSQLLocalDB"
    }
}

if ([string]::IsNullOrEmpty($databaseName))
{
    $databaseName = "ChurchBulletin"
}

Invoke-AcceptanceTests -databaseServer $databaseServer -databaseName $databaseName