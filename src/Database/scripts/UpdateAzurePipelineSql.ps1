param(
	[string]$DatabaseServer,
	[string]$DatabaseName,
	[string]$DatabaseAction,
	[string]$DatabaseUser,
	[string]$DatabasePassword
)

Write-Output "Recursive directory listing for diagnostics"
#Get-ChildItem -Recurse

Write-Host "Executing & .\scripts\AliaSQL.exe $DatabaseAction $DatabaseServer $DatabaseName .\scripts $DatabaseUser $DatabasePassword"

& .\scripts\AliaSQL.exe $DatabaseAction $DatabaseServer $DatabaseName .\scripts $DatabaseUser $DatabasePassword

if ($lastexitcode -ne 0) {
    throw ("AliaSQL had an error.")
}