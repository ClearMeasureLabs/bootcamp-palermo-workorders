#Requires -Version 7.0

<#
.SYNOPSIS
    Gets project details from Octopus Deploy using the REST API.

.DESCRIPTION
    Queries the Octopus Deploy REST API to retrieve project information by name.
    Requires OCTOPUS_URL and OCTOPUS_API_KEY environment variables to be set.

.PARAMETER ProjectName
    The name of the project to retrieve. Defaults to 'ChurchBulletin-gh'.

.PARAMETER SpaceId
    The Octopus space ID. Defaults to 'Spaces-315'.

.EXAMPLE
    .\Get-OctopusProject.ps1

.EXAMPLE
    .\Get-OctopusProject.ps1 -ProjectName "MyProject" -SpaceId "Spaces-1"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectName = "ChurchBulletin-gh",
    
    [Parameter(Mandatory = $false)]
    [string]$SpaceId = "Spaces-315"
)

$ErrorActionPreference = "Stop"

# Validate environment variables
if ([string]::IsNullOrWhiteSpace($env:OCTOPUS_URL)) {
    Write-Error "OCTOPUS_URL environment variable is not set"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($env:OCTOPUS_API_KEY)) {
    Write-Error "OCTOPUS_API_KEY environment variable is not set"
    exit 1
}

# Set up API request
$headers = @{
    "X-Octopus-ApiKey" = $env:OCTOPUS_API_KEY
}

$octopusUrl = $env:OCTOPUS_URL
$apiUrl = "$octopusUrl/api/$SpaceId/projects?name=$ProjectName"

Write-Host "=== Querying Octopus Deploy API ===" -ForegroundColor Cyan
Write-Host "URL: $apiUrl" -ForegroundColor Yellow
Write-Host ""

try {
    # Call the API
    $response = Invoke-RestMethod -Uri $apiUrl -Headers $headers -Method Get
    
    # Display summary
    Write-Host "=== API Response Summary ===" -ForegroundColor Green
    Write-Host "Total Results: $($response.TotalResults)"
    Write-Host "Items Per Page: $($response.ItemsPerPage)"
    Write-Host ""
    
    if ($response.TotalResults -eq 0) {
        Write-Host "No project found with name '$ProjectName' in space $SpaceId" -ForegroundColor Yellow
        exit 0
    }
    
    # Get the project (should be only one with exact name match)
    $project = $response.Items[0]
    
    Write-Host "=== Project Details ===" -ForegroundColor Green
    Write-Host "Name: $($project.Name)"
    Write-Host "ID: $($project.Id)"
    Write-Host "Slug: $($project.Slug)"
    Write-Host "Description: $($project.Description)"
    Write-Host "Is Disabled: $($project.IsDisabled)"
    Write-Host "Is Version Controlled: $($project.IsVersionControlled)"
    Write-Host ""
    
    if ($project.IsVersionControlled) {
        Write-Host "=== Version Control Settings ===" -ForegroundColor Green
        Write-Host "Repository URL: $($project.PersistenceSettings.Url)"
        Write-Host "Default Branch: $($project.PersistenceSettings.DefaultBranch)"
        Write-Host "Base Path: $($project.PersistenceSettings.BasePath)"
        Write-Host "Credentials Type: $($project.PersistenceSettings.Credentials.Type)"
        Write-Host "Variables in Git: $($project.PersistenceSettings.ConversionState.VariablesAreInGit)"
        Write-Host "Runbooks in Git: $($project.PersistenceSettings.ConversionState.RunbooksAreInGit)"
        Write-Host ""
    }
    
    Write-Host "=== Project Configuration ===" -ForegroundColor Green
    Write-Host "Lifecycle ID: $($project.LifecycleId)"
    Write-Host "Project Group ID: $($project.ProjectGroupId)"
    Write-Host "Variable Set ID: $($project.VariableSetId)"
    Write-Host "Deployment Process ID: $($project.DeploymentProcessId)"
    Write-Host ""
    
    Write-Host "=== Useful API Links ===" -ForegroundColor Green
    Write-Host "Releases: $octopusUrl$($project.Links.Releases)"
    Write-Host "Variables: $octopusUrl$($project.Links.Variables)"
    Write-Host "Deployment Process: $octopusUrl$($project.Links.DeploymentProcess)"
    Write-Host "Progression: $octopusUrl$($project.Links.Progression)"
    Write-Host "Web UI: $octopusUrl$($project.Links.Web)"
    Write-Host ""
    
    Write-Host "=== Full JSON Response ===" -ForegroundColor Cyan
    Write-Host "To see the full JSON response, run:"
    Write-Host "  `$response | ConvertTo-Json -Depth 10" -ForegroundColor Gray
    Write-Host ""
    
    # Return the response for further use
    return $response 
    
} catch {
    Write-Error "Failed to query Octopus API: $($_.Exception.Message)"
    exit 1
}
