$DatabaseServer = $OctopusParameters["DatabaseServer"]
$DatabaseName = $OctopusParameters["DatabaseName"]
$DatabaseAction = $OctopusParameters["DatabaseAction"]
$DatabaseUser = $OctopusParameters["DatabaseUser"]
$DatabasePassword = $OctopusParameters["DatabasePassword"]



# Get the package root directory - if script is in scripts folder, go up one level
# $PSScriptRoot is the directory where this script is located
if ($PSScriptRoot -match '\\scripts$' -or $PSScriptRoot -match '/scripts$') {
    $packageRoot = Split-Path -Parent $PSScriptRoot
} else {
    $packageRoot = $PSScriptRoot
}

# Find the database assembly in the package root
$databaseAssembly = Get-ChildItem -Path $packageRoot -Filter "ClearMeasure.Bootcamp.Database.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

# The scripts directory is where this script is located
$scriptDir = $PSScriptRoot

if (-not $databaseAssembly) {
    throw "Could not find ClearMeasure.Bootcamp.Database.dll in $packageRoot or its subfolders"
}

Write-Host "Update Azure SQL "
Write-Host "  Package Root: $packageRoot"
Write-Host "  Script Dir: $scriptDir"
Write-Host "  Database Assembly: $databaseAssembly"
Write-Host "  Database Action: $DatabaseAction"
Write-Host "  Database Server: $DatabaseServer"
Write-Host "  Database Name: $DatabaseName"
Write-Host "  Database P: DatabasePassword"
dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}