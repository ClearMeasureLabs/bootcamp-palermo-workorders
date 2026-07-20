#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Launch an interactive AI Factory dev-workstation container: bind-mounts the
    host repo, runs PrivateBuild (SQL Server via Docker-in-Docker), then idles
    ready for `docker exec` / serve-app.
.EXAMPLE
    ./run-dev.ps1
    ./run-dev.ps1 -HttpsPort 7174 -HttpPort 5174 -ContainerName ai-factory-dev
.NOTES
    After it reports READY:
      ./enter-dev.ps1                       # terminal into the container
      (inside) pwsh /usr/local/bin/serve-app.ps1
      browse https://localhost:<HttpsPort>  # accept the self-signed cert
#>
[CmdletBinding()]
param(
    # Host ports to publish. 0 = auto-pick a free port.
    [int]$HttpsPort = 0,
    [int]$HttpPort  = 0,

    [string]$ContainerName = "ai-factory-dev",

    # Host repo to bind-mount at /workspace (defaults to this repo's root).
    [string]$HostRepo,

    [ValidateSet("claude", "bob")]
    [string]$AiAgent = "claude",

    [int]$CpuLimit = 16,
    [string]$MemoryLimit = "24g"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "container-manager.ps1")

function Get-FreePort {
    $l = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $l.Start(); $p = $l.LocalEndpoint.Port; $l.Stop(); return $p
}

if ($HttpsPort -eq 0) { $HttpsPort = Get-FreePort }
if ($HttpPort  -eq 0) { $HttpPort  = Get-FreePort }

# Resolve the host repo path (default: three levels up from this skill dir -> repo root)
if (-not $HostRepo) {
    $HostRepo = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}
if (-not (Test-Path (Join-Path $HostRepo "build.ps1"))) {
    throw "HostRepo '$HostRepo' does not look like the repo root (no build.ps1)."
}

Initialize-AgentImage
Initialize-AgentNetwork
$tokenFile = Initialize-GitHubSecret
$agentSecret = Initialize-AgentSecret -AiAgent $AiAgent

# Remove any prior dev container with the same name
docker rm -f $ContainerName 2>$null | Out-Null

Write-Progress "Launching dev workstation: $ContainerName"

$dockerArgs = @(
    "run", "-dit",
    "--name", $ContainerName,
    "--network", $script:NetworkName,
    "--privileged",                       # Docker-in-Docker for SQL Server
    "--cpus", $CpuLimit,
    "--memory", $MemoryLimit,
    "-p", "${HttpsPort}:7174",
    "-p", "${HttpPort}:5174",
    "-e", "AI_AGENT=$AiAgent",
    "-e", "ENABLE_DIND=true",
    "-e", "REPO_ORG=ClearMeasureLabs",
    "-e", "REPO_NAME=bootcamp-palermo-workorders",
    "-v", "${HostRepo}:/workspace",
    "-v", "ai-factory-nuget:/tmp/nuget-packages",
    "-v", "${tokenFile}:/run/secrets/gh_token:ro",
    "-v", "$($agentSecret.HostPath):$($agentSecret.ContainerPath):ro",
    "--entrypoint", "pwsh",
    $script:ImageName, "/usr/local/bin/dev-entrypoint.ps1"
)

$containerId = docker @dockerArgs 2>&1
if ($LASTEXITCODE -ne 0) {
    if ($agentSecret.Temp -and (Test-Path $agentSecret.HostPath)) { Remove-Item $agentSecret.HostPath -Force }
    if (Test-Path $tokenFile) { Remove-Item $tokenFile -Force }
    throw "Failed to start dev container: $containerId"
}

Write-Success "Dev workstation started: $ContainerName"
Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " AI FACTORY DEV WORKSTATION" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " Repo (bind-mounted): $HostRepo -> /workspace" -ForegroundColor White
Write-Host " Setup in progress:   PrivateBuild + SQL Server (watch: docker logs -f $ContainerName)" -ForegroundColor White
Write-Host ""
Write-Host " Terminal in:   ./enter-dev.ps1 -ContainerName $ContainerName" -ForegroundColor Cyan
Write-Host "   (or)         docker exec -it $ContainerName pwsh" -ForegroundColor Cyan
Write-Host " Start the app: (inside) pwsh /usr/local/bin/serve-app.ps1" -ForegroundColor Cyan
Write-Host " Browse (host): https://localhost:$HttpsPort   (accept self-signed cert)" -ForegroundColor Cyan
Write-Host "                http://localhost:$HttpPort" -ForegroundColor Cyan
Write-Host " Stop:          docker rm -f $ContainerName" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Green

return @{ ContainerName = $ContainerName; HttpsPort = $HttpsPort; HttpPort = $HttpPort; HostRepo = $HostRepo }
