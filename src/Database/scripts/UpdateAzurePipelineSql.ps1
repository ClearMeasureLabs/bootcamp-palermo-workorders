param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

$databaseAssembly = Get-ChildItem -Path $PWD -Filter "ClearMeasure.Bootcamp.Database.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

# Resolve scripts directory relative to package root ($PWD), not current script location
# The package structure has scripts in a 'scripts' folder at the root level
$scriptsPath = Join-Path $PWD "scripts"
if (-not (Test-Path $scriptsPath)) {
    # Try lowercase 'Scripts' as fallback (Windows is case-insensitive but PowerShell might need explicit path)
    $scriptsPath = Join-Path $PWD "Scripts"
}
if (-not (Test-Path $scriptsPath)) {
    throw "Could not find scripts directory. Looked in: $($PWD)\scripts and $($PWD)\Scripts"
}
$scriptDir = Resolve-Path -Path $scriptsPath

if (-not $databaseAssembly) {
    throw "Could not find ClearMeasure.Bootcamp.Database.dll in $PWD or its subfolders"
}

# Made it this far, so proceed with the database action.
Write-Host "Update Azure Pipeline SQL "
Write-Host "  Package Root: $packageRoot"
Write-Host "  Script Dir: $scriptDir"
Write-Host "  Database Assembly: $databaseAssembly"
Write-Host "  Database Action: $DatabaseAction"
Write-Host "  Database Server: $DatabaseServer"
Write-Host "  Database Name: $DatabaseName"
Write-Host "  Database P: $DatabasePassword"
dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
if ($lastexitcode -ne 0) {
    throw ("Database migrations had an error.")
}