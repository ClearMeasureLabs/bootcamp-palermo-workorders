#
# UpdateAzureSQL.ps1
#
$DatabaseServer = $OctopusParameters["DatabaseServer"]
$DatabaseName = $OctopusParameters["DatabaseName"]
$DatabaseAction = $OctopusParameters["DatabaseAction"]
$DatabaseUser = $OctopusParameters["DatabaseUser"]
$DatabasePassword = $OctopusParameters["DatabasePassword"]


$scriptDir = Join-Path $PWD "scripts"
$bootcampdatabaseDll = Join-Path $PWD "ClearMeasure.Bootcamp.Database.dll"

Write-Host "Executing dotnet $bootcampdatabaseDll $DatabaseAction $DatabaseServer $DatabaseName $scriptDir"
dotnet $bootcampdatabaseDll $DatabaseAction $DatabaseServer $DatabaseName $scriptDir
if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}