#Requires -Version 7.0

<#
.SYNOPSIS
    Inspects ui-gh container apps across all bootcamp environments.

.DESCRIPTION
    This script checks the status of ui-gh container apps in bootcamp-tdd, bootcamp-uat, 
    and bootcamp-prod resource groups. It displays:
    - Environment variable ConnectionStrings__SqlConnectionString
    - Docker image version
    - FQDN
    - Container app status

.EXAMPLE
    .\inspect-container-apps.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Define the environments to check
$environments = @(
    @{ Name = "TDD"; ResourceGroup = "bootcamp-tdd" }
    @{ Name = "UAT"; ResourceGroup = "bootcamp-uat" }
    @{ Name = "PROD"; ResourceGroup = "bootcamp-prod" }
)

$containerAppName = "ui-gh"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Container App Inspection Report" -ForegroundColor Cyan
Write-Host "Container App: $containerAppName" -ForegroundColor Cyan
Write-Host "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($env in $environments) {
    Write-Host "========================================"
    Write-Host "Environment: $($env.Name)" -ForegroundColor Yellow
    Write-Host "Resource Group: $($env.ResourceGroup)" -ForegroundColor Yellow
    Write-Host "========================================"
    
    try {
        # Get container app details
        $appDetails = az containerapp show `
            --resource-group $env.ResourceGroup `
            --name $containerAppName `
            --query "{Status:properties.runningStatus, FQDN:properties.configuration.ingress.fqdn, Image:properties.template.containers[0].image, LatestRevision:properties.latestRevisionName, MinReplicas:properties.template.scale.minReplicas, MaxReplicas:properties.template.scale.maxReplicas}" `
            --output json | ConvertFrom-Json
        
        # Get connection string
        $connectionString = az containerapp show `
            --resource-group $env.ResourceGroup `
            --name $containerAppName `
            --query "properties.template.containers[0].env[?name=='ConnectionStrings__SqlConnectionString'].value" `
            --output tsv
        
        # Display results
        Write-Host ""
        Write-Host "Status:" -ForegroundColor Green
        Write-Host "  Running Status: $($appDetails.Status)"
        Write-Host "  Latest Revision: $($appDetails.LatestRevision)"
        Write-Host "  Scale: Min $($appDetails.MinReplicas) / Max $($appDetails.MaxReplicas) replicas"
        
        Write-Host ""
        Write-Host "Network:" -ForegroundColor Green
        Write-Host "  FQDN: $($appDetails.FQDN)"
        Write-Host "  URL: https://$($appDetails.FQDN)"
        
        Write-Host ""
        Write-Host "Container:" -ForegroundColor Green
        Write-Host "  Image: $($appDetails.Image)"
        
        # Extract version from image
        if ($appDetails.Image -match ':(.+)$') {
            Write-Host "  Version: $($matches[1])" -ForegroundColor Cyan
        }
        
        Write-Host ""
        Write-Host "Connection String:" -ForegroundColor Green
        if ([string]::IsNullOrWhiteSpace($connectionString)) {
            Write-Host "  ⚠️  NOT SET" -ForegroundColor Red
        } else {
            # Parse connection string to show key details
            if ($connectionString -match 'Server=tcp:([^,;]+)') {
                Write-Host "  Server: $($matches[1])"
            }
            if ($connectionString -match 'Initial Catalog=([^;]+)') {
                Write-Host "  Database: $($matches[1])"
            }
            if ($connectionString -match 'User ID=([^;]+)') {
                Write-Host "  User: $($matches[1])"
            }
            if ($connectionString -match 'Password=([^;]+)') {
                Write-Host "  Password: ******** (hidden)"
            }
            
            # Show full connection string (with password masked)
            $maskedConnectionString = $connectionString -replace 'Password=[^;]+', 'Password=********'
            Write-Host ""
            Write-Host "  Full Connection String:" -ForegroundColor Gray
            Write-Host "  $maskedConnectionString" -ForegroundColor DarkGray
        }
        
    } catch {
        Write-Host "❌ Error checking $($env.Name): $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Inspection Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
