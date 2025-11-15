param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

$scriptDir = Join-Path $PWD "scripts"
$bootcampdatabaseDll = Join-Path $PWD "ClearMeasure.Bootcamp.Database.dll"

Write-Host "Executing dotnet $bootcampdatabaseDll $DatabaseAction $DatabaseServer $DatabaseName $scriptDir"

dotnet $bootcampdatabaseDll $DatabaseAction $DatabaseServer $DatabaseName $scriptDir $DatabaseUser $DatabasePassword

if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}