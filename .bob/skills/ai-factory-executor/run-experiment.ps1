#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Launches an AI Factory experiment container running the agent in an
    attachable tmux session for remote inspection / control.
.EXAMPLE
    ./run-experiment.ps1 -InitPrompt "Explore the WorkOrder domain model and propose 3 refactors" -TimeoutMinutes 60
.DESCRIPTION
    After launch, control/inspect the running agent from the host:
        docker exec -it <name> tmux attach -t agent    # take control of the live session
        docker exec -it <name> bash                    # inspect the workspace
        docker exec -it <name> claude --resume         # inspect the transcript
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InitPrompt,

    [ValidateSet("claude", "bob")]
    [string]$AiAgent = "claude",

    [int]$TimeoutMinutes = 60,

    # Display name for the Remote Control session (as it appears in the Claude app)
    [string]$RemoteControlName = "ai-factory-experiment",

    [string]$RepoOrg = "ClearMeasureLabs",

    [string]$RepoName = "bootcamp-palermo-workorders",

    [int]$CpuLimit = 16,

    [string]$MemoryLimit = "24g"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "container-manager.ps1")

Initialize-AgentImage
Initialize-AgentNetwork
$tokenFile = Initialize-GitHubSecret
$agentSecret = Initialize-AgentSecret -AiAgent $AiAgent

$containerName = "ai-factory-experiment-$(Get-Random -Maximum 9999)"

Write-Progress "Launching experiment container: $containerName"

$containerId = docker run -d `
    --name $containerName `
    --network $script:NetworkName `
    --cpus $CpuLimit `
    --memory $MemoryLimit `
    --entrypoint pwsh `
    -e "AI_AGENT=$AiAgent" `
    -e "INIT_PROMPT=$InitPrompt" `
    -e "REMOTE_CONTROL_NAME=$RemoteControlName" `
    -e "REPO_ORG=$RepoOrg" `
    -e "REPO_NAME=$RepoName" `
    -e "EXPERIMENT_TIMEOUT_MINUTES=$TimeoutMinutes" `
    -v "${tokenFile}:/run/secrets/gh_token:ro" `
    -v "$($agentSecret.HostPath):$($agentSecret.ContainerPath):ro" `
    -v "ai-factory-nuget:/home/bobagent/.nuget/packages" `
    $script:ImageName /usr/local/bin/experiment-entrypoint.ps1 2>&1

if ($LASTEXITCODE -ne 0) {
    if ($agentSecret.Temp -and (Test-Path $agentSecret.HostPath)) { Remove-Item $agentSecret.HostPath -Force }
    if (Test-Path $tokenFile) { Remove-Item $tokenFile -Force }
    throw "Failed to start experiment container: $containerId"
}

Write-Success "Experiment container started: $containerName"
Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " REMOTE CONTROL / INSPECTION" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " Agent:   $AiAgent" -ForegroundColor White
Write-Host " Prompt:  $InitPrompt" -ForegroundColor White
Write-Host " Up for:  $TimeoutMinutes minutes" -ForegroundColor White
Write-Host ""
Write-Host " >> iPhone app: open the Claude app and look for the Remote Control" -ForegroundColor Yellow
Write-Host "    session named '$RemoteControlName'. Drive the agent from there." -ForegroundColor Yellow
Write-Host ""
Write-Host " Local inspection options:" -ForegroundColor White
Write-Host " Take control of the live agent session (local terminal):" -ForegroundColor White
Write-Host "   docker exec -it $containerName tmux attach -t agent" -ForegroundColor Cyan
Write-Host " Inspect the workspace / filesystem:" -ForegroundColor White
Write-Host "   docker exec -it $containerName bash" -ForegroundColor Cyan
Write-Host " Inspect the agent transcript:" -ForegroundColor White
Write-Host "   docker exec -it $containerName claude --resume" -ForegroundColor Cyan
Write-Host " Follow container logs:" -ForegroundColor White
Write-Host "   docker logs -f $containerName" -ForegroundColor Cyan
Write-Host " Detach from tmux (leaves agent running): Ctrl-b then d" -ForegroundColor White
Write-Host " Stop early:" -ForegroundColor White
Write-Host "   docker rm -f $containerName" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Green

# Note: secret temp files are left in place while the container runs (it reads
# them at startup and copies credentials internally). They are cleaned up by the
# OS temp policy; the mounts are read-only.

return @{ ContainerName = $containerName; ContainerId = $containerId.Trim() }
