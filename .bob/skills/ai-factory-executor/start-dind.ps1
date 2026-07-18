#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts an in-container Docker daemon (Docker-in-Docker) and waits until it
    is ready. No-op unless ENABLE_DIND=true. Requires the container to run with
    --privileged. Intended to be dot-sourced or invoked by an entrypoint before
    the agent / build runs.
.OUTPUTS
    Sets exit behavior via exceptions; on success `docker info` works for bobagent.
#>
[CmdletBinding()]
param(
    [int]$TimeoutSeconds = 60
)

function Write-DindInfo { param([string]$m) Write-Host "[dind] $m" -ForegroundColor Yellow }

if ($env:ENABLE_DIND -ne "true") {
    Write-DindInfo "ENABLE_DIND != true - skipping Docker-in-Docker startup"
    return
}

Write-DindInfo "Starting in-container Docker daemon..."

# dockerd must run as root; bobagent has passwordless sudo. Log to a file.
$dockerdLog = "/tmp/dockerd.log"
Start-Process -FilePath "sudo" `
    -ArgumentList @("dockerd", "--host=unix:///var/run/docker.sock", "--storage-driver=fuse-overlayfs") `
    -RedirectStandardOutput $dockerdLog -RedirectStandardError "/tmp/dockerd.err" `
    -NoNewWindow

# Make the socket usable by bobagent without sudo for every call.
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$ready = $false
while ((Get-Date) -lt $deadline) {
    if (Test-Path "/var/run/docker.sock") {
        sudo chmod 666 /var/run/docker.sock 2>$null
        docker info *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    Write-DindInfo "Docker daemon did not become ready within ${TimeoutSeconds}s. Recent dockerd log:"
    if (Test-Path "/tmp/dockerd.err") { Get-Content "/tmp/dockerd.err" -Tail 30 | ForEach-Object { Write-Host "  $_" } }
    throw "Failed to start Docker-in-Docker daemon"
}

$dockerVersion = (docker version --format '{{.Server.Version}}' 2>$null)
Write-DindInfo "Docker daemon ready (server $dockerVersion)"
