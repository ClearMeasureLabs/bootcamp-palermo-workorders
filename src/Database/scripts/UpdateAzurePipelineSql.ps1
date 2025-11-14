param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

Write-Output "Recursive directory listing for diagnostics"
Get-ChildItem -Recurse

Write-Host "Executing dotnet .\scripts\ClearMeasure.Bootcamp.Database.dll $DatabaseAction $DatabaseServer $DatabaseName .\scripts"

dotnet .\scripts\ClearMeasure.Bootcamp.Database.dll $DatabaseAction $DatabaseServer $DatabaseName .\scripts

if ($lastexitcode -ne 0) {
    throw ("Database migration had an error.")
}