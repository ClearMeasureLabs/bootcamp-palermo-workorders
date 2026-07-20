#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Open an interactive terminal inside a running AI Factory dev container.
.EXAMPLE
    ./enter-dev.ps1                       # docker exec into ai-factory-dev
    ./enter-dev.ps1 -Shell bash
    ./enter-dev.ps1 -NewWindow            # open in a new Windows Terminal tab
#>
[CmdletBinding()]
param(
    [string]$ContainerName = "ai-factory-dev",
    [ValidateSet("pwsh", "bash")]
    [string]$Shell = "pwsh",
    [switch]$NewWindow
)

$running = docker ps --filter "name=^${ContainerName}$" --format "{{.Names}}" 2>$null
if (-not $running) {
    Write-Host "[ERROR] Container '$ContainerName' is not running. Start it with ./run-dev.ps1" -ForegroundColor Red
    exit 1
}

if ($NewWindow -and (Get-Command wt.exe -ErrorAction SilentlyContinue)) {
    # Open in a new Windows Terminal tab
    wt.exe -w 0 new-tab --title "$ContainerName" docker exec -it $ContainerName $Shell
} else {
    docker exec -it $ContainerName $Shell
}
