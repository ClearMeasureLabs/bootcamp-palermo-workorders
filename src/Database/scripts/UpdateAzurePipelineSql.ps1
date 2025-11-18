param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

$databaseAssembly = Get-ChildItem -Path $PWD -Filter "ClearMeasure.Bootcamp.Database.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
$scriptDir = Resolve-Path -Path (".\scripts")

if (-not $databaseAssembly) {
    throw "Could not find ClearMeasure.Bootcamp.Database.dll in $PWD or its subfolders"
}

# Check if database has been baselined by checking for SchemaVersions table
$connectionString = "Server=$DatabaseServer;Database=$DatabaseName;User Id=$DatabaseUser;Password=$DatabasePassword;TrustServerCertificate=True"

$checkSchemaQuery = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions'
) THEN 1 ELSE 0 END AS TableExists
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $checkSchemaQuery
    $tableExists = $command.ExecuteScalar()
    
    $connection.Close()
    
    if ($tableExists -eq 1) {
        Write-Host "Database has been baselined - SchemaVersions table exists"
        
        # Check for existing records
        $connection.Open()
        $command.CommandText = "SELECT COUNT(*) FROM dbo.SchemaVersions"
        $recordCount = $command.ExecuteScalar()
        $connection.Close()
        
        # Count SQL files in scripts directory
        $sqlFileCount = (Get-ChildItem -Path $scriptDir -Filter "*.sql" -Recurse -File).Count
        
        Write-Host "SchemaVersions table contains $recordCount script record(s)"
        Write-Host "Scripts directory contains $sqlFileCount SQL file(s)"
        
        if ($recordCount -lt $sqlFileCount) {
            Write-Warning "Database may not be fully baselined: dbo.SchemaVersions has $recordCount records vs $sqlFileCount SQL files"
        	Write-Host "Baselining database dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"
        	dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
        	if ($lastexitcode -ne 0) {
            	throw ("Database baseline had an error.")
        	}			
        } elseif ($recordCount -eq $sqlFileCount) {
            Write-Host "Database appears fully baselined: record count matches SQL file count" -ForegroundColor Cyan
        } else {
            Write-Warning "Database has more records than SQL files: $recordCount records vs $sqlFileCount files"
        }
    } else {
        Write-Host "Database not baselined - SchemaVersions table does not exist. Running baseline..."
        Write-Host "Baselining database dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"
        dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
        if ($lastexitcode -ne 0) {
            throw ("Database baseline had an error.")
        }
    }
} catch {
    Write-Warning "Could not check for SchemaVersions table: $_"
    Write-Host "Attempting baseline anyway..."
    Write-Host "Baselining database dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"
    dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
    if ($lastexitcode -ne 0) {
        throw ("Database baseline had an error.")
    }
}

# Made it this far, so proceed with the database action.
Write-Host "Executing dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"
dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
if ($lastexitcode -ne 0) {
    throw ("Database migrations had an error.")
}