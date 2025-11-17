$DatabaseServer = $OctopusParameters["DatabaseServer"]
$DatabaseName = $OctopusParameters["DatabaseName"]
$DatabaseAction = $OctopusParameters["DatabaseAction"]
$DatabaseUser = $OctopusParameters["DatabaseUser"]
$DatabasePassword = $OctopusParameters["DatabasePassword"]



$databaseAssembly = Get-ChildItem -Path $PWD -Filter "ClearMeasure.Bootcamp.Database.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
$scriptDir = Resolve-Path -Path (".\scripts")

if (-not $databaseAssembly) {
    throw "Could not find ClearMeasure.Bootcamp.Database.dll in $PWD or its subfolders"
}

Write-Host "Executing dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser <REDACTED>"
dotnet $databaseAssembly $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword
if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}