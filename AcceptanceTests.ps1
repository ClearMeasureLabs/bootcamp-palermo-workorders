param (
    [Parameter(Mandatory=$false)]
    [ValidateNotNullOrEmpty()]
    [string]$databaseServer = "(LocalDb)\MSSQLLocalDB"
)

. .\build.ps1

Run-AcceptanceTests -databaseServer $databaseServer