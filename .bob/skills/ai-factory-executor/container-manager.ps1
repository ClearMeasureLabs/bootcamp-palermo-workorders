#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Container lifecycle management for AI Factory agents
.DESCRIPTION
    Spawns, monitors, and manages Docker containers that run Bob CLI agents
    for autonomous GitHub issue implementation.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Configuration
$script:ImageName = "ai-factory-agent:latest"
$script:NetworkName = "ai-factory"
$script:LogsDir = Join-Path $PSScriptRoot "logs"

# Ensure logs directory exists
if (-not (Test-Path $script:LogsDir)) {
    New-Item -ItemType Directory -Path $script:LogsDir | Out-Null
}

# Color output helpers
function Write-Success { param([string]$Message) Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }
function Write-Progress { param([string]$Message) Write-Host "[->] $Message" -ForegroundColor Cyan }
function Write-Info { param([string]$Message) Write-Host "[INFO] $Message" -ForegroundColor Yellow }

<#
.SYNOPSIS
    Ensures the Docker image is built and ready
#>
function Initialize-AgentImage {
    Write-Progress "Checking Docker image: $script:ImageName"
    
    $imageExists = docker images -q $script:ImageName 2>$null
    
    if (-not $imageExists) {
        Write-Progress "Building Docker image..."
        
        $dockerfilePath = Join-Path $PSScriptRoot "Dockerfile.agent"
        
        if (-not (Test-Path $dockerfilePath)) {
            throw "Dockerfile.agent not found at $dockerfilePath"
        }
        
        docker build -t $script:ImageName -f $dockerfilePath $PSScriptRoot
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build Docker image"
        }
        
        Write-Success "Docker image built"
    } else {
        Write-Success "Docker image ready"
    }
}

<#
.SYNOPSIS
    Ensures the Docker network exists
#>
function Initialize-AgentNetwork {
    $networkExists = docker network ls --filter "name=$script:NetworkName" --format "{{.Name}}" 2>$null
    
    if (-not $networkExists) {
        Write-Progress "Creating Docker network: $script:NetworkName"
        docker network create $script:NetworkName | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create Docker network"
        }
        
        Write-Success "Docker network created"
    }
}

<#
.SYNOPSIS
    Prepares GitHub token as Docker secret
#>
function Initialize-GitHubSecret {
    Write-Progress "Preparing GitHub credentials..."
    
    # Get GitHub token from gh CLI
    $token = gh auth token 2>$null
    
    if ($LASTEXITCODE -ne 0 -or -not $token) {
        throw "Failed to get GitHub token. Run 'gh auth login' first."
    }
    
    # Create temporary token file
    $tokenFile = Join-Path $env:TEMP "gh_token_$(Get-Random).txt"
    Set-Content -Path $tokenFile -Value $token -NoNewline
    
    Write-Success "GitHub credentials prepared"

    return $tokenFile
}

<#
.SYNOPSIS
    Prepares AI agent credentials as a Docker secret file.
.DESCRIPTION
    Returns a hashtable describing the secret to mount for the selected agent:
      @{ HostPath = <file>; ContainerPath = </run/secrets/...>; Temp = <bool> }
    'Temp' indicates whether HostPath is a throwaway file to delete on cleanup.
#>
function Initialize-AgentSecret {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AiAgent
    )

    switch ($AiAgent.ToLower()) {
        "bob" {
            Write-Progress "Preparing Bob Shell credentials..."
            $key = $env:BOBSHELL_API_KEY
            if (-not $key) {
                throw "BOBSHELL_API_KEY environment variable is not set on the host. Bob Shell headless auth requires it."
            }
            $keyFile = Join-Path $env:TEMP "bobshell_key_$(Get-Random).txt"
            Set-Content -Path $keyFile -Value $key -NoNewline
            Write-Success "Bob Shell credentials prepared"
            return @{ HostPath = $keyFile; ContainerPath = "/run/secrets/bobshell_api_key"; Temp = $true }
        }
        "claude" {
            Write-Progress "Preparing Claude Code credentials..."
            # Prefer an explicit API key if present; otherwise mount OAuth credentials.
            if ($env:ANTHROPIC_API_KEY) {
                $keyFile = Join-Path $env:TEMP "anthropic_key_$(Get-Random).txt"
                Set-Content -Path $keyFile -Value $env:ANTHROPIC_API_KEY -NoNewline
                Write-Success "Claude API key prepared"
                return @{ HostPath = $keyFile; ContainerPath = "/run/secrets/anthropic_api_key"; Temp = $true }
            }
            $credPath = Join-Path $env:USERPROFILE ".claude/.credentials.json"
            if (-not (Test-Path $credPath)) {
                throw "No Claude credentials found. Set ANTHROPIC_API_KEY or log in with 'claude' so $credPath exists."
            }
            Write-Success "Claude OAuth credentials located"
            return @{ HostPath = $credPath; ContainerPath = "/run/secrets/claude_credentials"; Temp = $false }
        }
        default {
            throw "Unknown AI agent '$AiAgent'. Supported values: claude, bob."
        }
    }
}

<#
.SYNOPSIS
    Starts a Docker container for an issue
#>
function Start-AgentContainer {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [int]$IssueNumber,
        
        [Parameter(Mandatory)]
        [string]$IssueTitle,
        
        [Parameter(Mandatory)]
        [string]$IssueBody,
        
        [Parameter(Mandatory)]
        [string]$IssueUrl,
        
        [Parameter(Mandatory)]
        [string]$BranchName,
        
        [Parameter(Mandatory)]
        [string]$RepoOrg,
        
        [Parameter(Mandatory)]
        [string]$RepoName,

        [ValidateSet("claude", "bob")]
        [string]$AiAgent = "claude",

        [bool]$MonitorChecks = $true,

        [bool]$RunQualityGates = $true,

        [int]$CpuLimit = 16,

        [string]$MemoryLimit = "24g"
    )

    # Initialize infrastructure
    Initialize-AgentImage
    Initialize-AgentNetwork
    $tokenFile = Initialize-GitHubSecret
    $agentSecret = Initialize-AgentSecret -AiAgent $AiAgent
    
    try {
        # Generate container name
        $containerName = "ai-factory-issue-$IssueNumber-$(Get-Random -Maximum 9999)"
        
        # Prepare log file
        $logFile = Join-Path $script:LogsDir "issue-$IssueNumber.log"
        
        Write-Progress "Starting container: $containerName"
        
        # Start container
        $containerId = docker run -d `
            --name $containerName `
            --network $script:NetworkName `
            --cpus $CpuLimit `
            --memory $MemoryLimit `
            -e "ISSUE_NUMBER=$IssueNumber" `
            -e "ISSUE_TITLE=$IssueTitle" `
            -e "ISSUE_BODY=$IssueBody" `
            -e "ISSUE_URL=$IssueUrl" `
            -e "BRANCH_NAME=$BranchName" `
            -e "REPO_ORG=$RepoOrg" `
            -e "REPO_NAME=$RepoName" `
            -e "AI_AGENT=$AiAgent" `
            -e "MONITOR_CHECKS=$($MonitorChecks.ToString().ToLower())" `
            -e "RUN_QUALITY_GATES=$($RunQualityGates.ToString().ToLower())" `
            -v "${tokenFile}:/run/secrets/gh_token:ro" `
            -v "$($agentSecret.HostPath):$($agentSecret.ContainerPath):ro" `
            -v "ai-factory-nuget:/home/bobagent/.nuget/packages" `
            $script:ImageName 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to start container: $containerId"
        }
        
        # Trim container ID
        $containerId = $containerId.Trim()
        
        Write-Success "Container started: $containerId"
        
        return @{
            ContainerId = $containerId
            ContainerName = $containerName
            IssueNumber = $IssueNumber
            LogFile = $logFile
            TokenFile = $tokenFile
            AgentSecret = $agentSecret
            StartTime = Get-Date
        }

    } catch {
        # Cleanup secret files on error
        if (Test-Path $tokenFile) {
            Remove-Item $tokenFile -Force
        }
        if ($agentSecret -and $agentSecret.Temp -and (Test-Path $agentSecret.HostPath)) {
            Remove-Item $agentSecret.HostPath -Force
        }
        throw
    }
}

<#
.SYNOPSIS
    Gets the status of a running container
#>
function Get-ContainerStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerId
    )
    
    $status = docker inspect --format '{{.State.Status}}' $ContainerId 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        return @{
            Status = "unknown"
            Running = $false
            ExitCode = $null
        }
    }
    
    $exitCode = docker inspect --format "{{.State.ExitCode}}" $ContainerId 2>$null
    
    return @{
        Status = $status
        Running = ($status -eq "running")
        ExitCode = if ($exitCode) { [int]$exitCode } else { $null }
    }
}

<#
.SYNOPSIS
    Retrieves logs from a container
#>
function Get-ContainerLogs {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ContainerId,
        
        [int]$TailLines = 0
    )
    
    if ($TailLines -gt 0) {
        $logs = docker logs --tail $TailLines $ContainerId 2>&1
    } else {
        $logs = docker logs $ContainerId 2>&1
    }
    
    return $logs
}

<#
.SYNOPSIS
    Waits for a container to complete
#>
function Wait-ContainerCompletion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Container,
        
        [int]$TimeoutMinutes = 30,
        
        [int]$PollIntervalSeconds = 10
    )
    
    $containerId = $Container.ContainerId
    $issueNumber = $Container.IssueNumber
    $logFile = $Container.LogFile
    $startTime = $Container.StartTime
    
    Write-Progress "Monitoring container for Issue #$issueNumber..."
    
    $lastLogPosition = 0
    
    while ($true) {
        # Check timeout
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalMinutes -gt $TimeoutMinutes) {
            Write-Failure "Container timeout (${TimeoutMinutes}m)"
            
            # Save logs before stopping
            $logs = Get-ContainerLogs -ContainerId $containerId
            Set-Content -Path $logFile -Value $logs
            
            # Stop container
            docker stop $containerId | Out-Null
            
            return @{
                Success = $false
                Error = "Timeout after $TimeoutMinutes minutes"
                Logs = $logs
            }
        }
        
        # Get container status
        $status = Get-ContainerStatus -ContainerId $containerId
        
        # Stream logs incrementally
        $currentLogs = Get-ContainerLogs -ContainerId $containerId
        # Convert to string to handle ErrorRecord objects
        $logsText = if ($currentLogs) { $currentLogs | Out-String } else { "" }
        if ($logsText.Length -gt $lastLogPosition) {
            $newLogs = $logsText.Substring($lastLogPosition)
            Write-Host $newLogs -NoNewline
            $lastLogPosition = $logsText.Length
        }
        
        # Check if container finished
        if (-not $status.Running) {
            # Save final logs
            $finalLogs = Get-ContainerLogs -ContainerId $containerId
            Set-Content -Path $logFile -Value $finalLogs
            
            $success = ($status.ExitCode -eq 0)
            
            if ($success) {
                Write-Success "Container completed successfully"
            } else {
                Write-Failure "Container failed with exit code $($status.ExitCode)"
            }
            
            return @{
                Success = $success
                ExitCode = $status.ExitCode
                Error = if (-not $success) { "Container exited with code $($status.ExitCode)" } else { $null }
                Logs = $finalLogs
                LogFile = $logFile
            }
        }
        
        # Wait before next check
        Start-Sleep -Seconds $PollIntervalSeconds
    }
}

<#
.SYNOPSIS
    Stops and removes a container
#>
function Stop-AgentContainer {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Container,
        
        [switch]$Force
    )
    
    $containerId = $Container.ContainerId
    $tokenFile = $Container.TokenFile
    $agentSecret = $Container.AgentSecret

    Write-Progress "Stopping container: $containerId"
    
    if ($Force) {
        docker kill $containerId 2>&1 | Out-Null
    } else {
        docker stop $containerId 2>&1 | Out-Null
    }
    
    # Remove container
    docker rm $containerId 2>&1 | Out-Null
    
    # Cleanup secret files (only delete throwaway temp files, never the user's real credentials)
    if ($tokenFile -and (Test-Path $tokenFile)) {
        Remove-Item $tokenFile -Force
    }
    if ($agentSecret -and $agentSecret.Temp -and (Test-Path $agentSecret.HostPath)) {
        Remove-Item $agentSecret.HostPath -Force
    }

    Write-Success "Container stopped and removed"
}

<#
.SYNOPSIS
    Cleans up all AI Factory containers
#>
function Clear-AllAgentContainers {
    [CmdletBinding()]
    param(
        [switch]$Force
    )
    
    Write-Progress "Cleaning up all AI Factory containers..."
    
    $containers = docker ps -a --filter "name=ai-factory-issue-" --format "{{.ID}}" 2>$null
    
    if ($containers) {
        foreach ($containerId in $containers) {
            if ($Force) {
                docker kill $containerId 2>&1 | Out-Null
            } else {
                docker stop $containerId 2>&1 | Out-Null
            }
            docker rm $containerId 2>&1 | Out-Null
        }
        
        Write-Success "Cleaned up $($containers.Count) containers"
    } else {
        Write-Info "No containers to clean up"
    }
}

# Functions are available via dot-sourcing (no Export-ModuleMember needed)
