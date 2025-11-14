#
# UpdateAzureSQL.ps1
#
$DatabaseServer = $OctopusParameters["DatabaseServer"]
$DatabaseName = $OctopusParameters["DatabaseName"]
$DatabaseAction = $OctopusParameters["DatabaseAction"]
$DatabaseUser = $OctopusParameters["DatabaseUser"]
$DatabasePassword = $OctopusParameters["DatabasePassword"]
Write-Output "Recursive directory listing for diagnostics"
Get-ChildItem -Recurse


$bootcampdatabaseDll = Join-Path ".\scripts" "ClearMeasure.Bootcamp.Database.dll"

Write-Host "Executing dotnet .\scripts\ClearMeasure.Bootcamp.Database.dll $DatabaseAction $DatabaseServer $DatabaseName .\scripts"
dotnet $bootcampdatabaseDll $DatabaseAction $DatabaseServer $DatabaseName .\scripts
if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}