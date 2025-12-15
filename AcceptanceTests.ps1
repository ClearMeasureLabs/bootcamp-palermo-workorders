param (
    [Parameter(Mandatory = $false)]
    [string]$databaseServer = "",

    [Parameter(Mandatory = $false)]
    [string]$databaseName = ""
)

. .\build.ps1


$connectionString = Get-ConnectionStringComponents

if ([string]::IsNullOrEmpty($databaseServer) -and [string]::IsNullOrEmpty($databaseName))  {
    # Try and use values from the environment variable.
    if (-not $connectionString.IsEmpty)
    {
        $databaseServer = $connectionString.Server
        $databaseName = $connectionString.Database
    }
}
else
{
    # Make some guesses.
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

    if ( [string]::IsNullOrEmpty($databaseName))
    {
        $databaseName = "ChurchBulletin"
    }
}

Invoke-AcceptanceTests -databaseServer $databaseServer -databaseName $databaseName