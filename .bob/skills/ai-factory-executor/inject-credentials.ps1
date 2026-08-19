#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Credential injection helper for AI Factory agent containers
.DESCRIPTION
    Prepares and injects GitHub CLI and Bob CLI credentials into Docker containers
    using secure methods (Docker secrets, volume mounts).
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

<#
.SYNOPSIS
    Exports GitHub CLI token to a temporary file
#>
function Export-GitHubToken {
    [CmdletBinding()]
    param(
        [string]$OutputPath
    )
    
    Write-Verbose "Exporting GitHub token..."
    
    # Get token from gh CLI
    $token = gh auth token 2>$null
    
    if ($LASTEXITCODE -ne 0 -or -not $token) {
        throw "Failed to get GitHub token. Ensure 'gh auth login' has been run."
    }
    
    # Validate token format
    if ($token -notmatch '^(ghp_|gho_|ghu_|ghs_|ghr_)') {
        throw "Invalid GitHub token format"
    }
    
    # Write to file
    if (-not $OutputPath) {
        $OutputPath = Join-Path $env:TEMP "gh_token_$(Get-Random).txt"
    }
    
    Set-Content -Path $OutputPath -Value $token -NoNewline -Force
    
    # Set restrictive permissions (Windows)
    if ($IsWindows -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue))) {
        $acl = Get-Acl $OutputPath
        $acl.SetAccessRuleProtection($true, $false)
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            [System.Security.Principal.WindowsIdentity]::GetCurrent().Name,
            "FullControl",
            "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl $OutputPath $acl
    }
    
    Write-Verbose "GitHub token exported to: $OutputPath"
    
    return $OutputPath
}

<#
.SYNOPSIS
    Exports Bob CLI configuration to a temporary directory
#>
function Export-BobConfig {
    [CmdletBinding()]
    param(
        [string]$OutputPath
    )
    
    Write-Verbose "Exporting Bob CLI configuration..."
    
    # Locate Bob CLI config directory
    $bobConfigDir = if ($env:BOB_CONFIG_DIR) {
        $env:BOB_CONFIG_DIR
    } elseif ($IsWindows -or (-not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue))) {
        Join-Path $env:USERPROFILE ".bob"
    } else {
        Join-Path $env:HOME ".bob"
    }
    
    if (-not (Test-Path $bobConfigDir)) {
        Write-Warning "Bob CLI config directory not found at: $bobConfigDir"
        return $null
    }
    
    # Create temporary directory
    if (-not $OutputPath) {
        $OutputPath = Join-Path $env:TEMP "bob_config_$(Get-Random)"
    }
    
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    # Copy config files
    Copy-Item -Path "$bobConfigDir\*" -Destination $OutputPath -Recurse -Force
    
    Write-Verbose "Bob CLI config exported to: $OutputPath"
    
    return $OutputPath
}

<#
.SYNOPSIS
    Cleans up temporary credential files
#>
function Remove-CredentialFiles {
    [CmdletBinding()]
    param(
        [string[]]$Paths
    )
    
    foreach ($path in $Paths) {
        if ($path -and (Test-Path $path)) {
            Write-Verbose "Removing credential file: $path"
            Remove-Item $path -Recurse -Force
        }
    }
}

<#
.SYNOPSIS
    Validates GitHub CLI authentication
#>
function Test-GitHubAuth {
    [CmdletBinding()]
    param()
    
    $status = gh auth status 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        return $false
    }
    
    return $true
}

<#
.SYNOPSIS
    Validates Bob CLI installation
#>
function Test-BobCli {
    [CmdletBinding()]
    param()
    
    $bobPath = Get-Command bob -ErrorAction SilentlyContinue
    
    return ($null -ne $bobPath)
}

# Functions are available via dot-sourcing (no Export-ModuleMember needed)
