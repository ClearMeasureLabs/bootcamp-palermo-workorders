param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

# Find ClearMeasure.Bootcamp.Database.dll in current folder or subfolders
$databaseAssembly = Get-ChildItem -Path $PWD -Filter "ClearMeasure.Bootcamp.Database.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $databaseAssembly) {
    throw "Could not find ClearMeasure.Bootcamp.Database.dll in $PWD or its subfolders"
}

Write-Host "Found database DLL at: $databaseAssembly"
$scriptDir = Resolve-Path -Path (".\scripts")
Write-Host "Executing dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"

# This is a one-off - we need to baseline the database the first time so that DbUp doesn't try to re-apply all the scripts
# in an existing database.
dotnet $databaseAssembly baseline $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword 
if ($lastexitcode -ne 0) {
    throw ("Failed to baseline the database.")
}

dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}