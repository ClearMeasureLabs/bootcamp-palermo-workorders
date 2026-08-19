#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Work Order app inside the dev container, bound to 0.0.0.0 so the
    published container ports are reachable from the Windows host browser.
.DESCRIPTION
    Binds Kestrel to all interfaces (0.0.0.0) - required for Docker port
    publishing to work (a localhost-only bind would refuse forwarded traffic) -
    and points the app at the SQL Server database prepared by dev-entrypoint
    (connection string captured in /home/bobagent/.dev-connection).
    Uses --no-launch-profile so ASPNETCORE_URLS is honored (launchSettings would
    otherwise re-bind to localhost).
.EXAMPLE
    pwsh /usr/local/bin/serve-app.ps1
    pwsh /usr/local/bin/serve-app.ps1 -HttpsPort 7174 -HttpPort 5174
#>
[CmdletBinding()]
param(
    [int]$HttpsPort = 7174,
    [int]$HttpPort  = 5174
)

$ErrorActionPreference = "Stop"
Set-Location /workspace

# Point the app at the DB that dev-entrypoint's PrivateBuild set up.
if (Test-Path "/home/bobagent/.dev-connection") {
    $env:ConnectionStrings__SqlConnectionString = (Get-Content "/home/bobagent/.dev-connection" -Raw).Trim()
    $env:DATABASE_ENGINE = "SqlServer"
    Write-Host "[INFO] Using SQL Server database from dev-entrypoint." -ForegroundColor Yellow
} else {
    Write-Host "[INFO] No captured DB connection; app will use its configured default." -ForegroundColor Yellow
}

# Bind to all interfaces so Docker-published ports reach Kestrel.
$env:ASPNETCORE_URLS = "https://0.0.0.0:$HttpsPort;http://0.0.0.0:$HttpPort"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " Starting Work Order app" -ForegroundColor Green
Write-Host " Container bind: $env:ASPNETCORE_URLS" -ForegroundColor White
Write-Host " From the Windows host: open the HTTPS port that run-dev.ps1 mapped" -ForegroundColor White
Write-Host "   (e.g. https://localhost:<mappedHttpsPort>) and accept the" -ForegroundColor White
Write-Host "   self-signed dev certificate. Health check: /_healthcheck" -ForegroundColor White
Write-Host "==================================================================" -ForegroundColor Green
Write-Host ""

dotnet run --project src/UI/Server --no-launch-profile --configuration Debug
