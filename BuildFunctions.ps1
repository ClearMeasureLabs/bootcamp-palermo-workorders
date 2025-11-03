# Taken from psake https://github.com/psake/psake

<#
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

Function Poke-Xml($filePath, $xpath, $value) {
    [xml] $fileXml = Get-Content $filePath
    $node = $fileXml.SelectSingleNode($xpath)
    
    if ($node.NodeType -eq "Element") {
        $node.InnerText = $value
    }
    else {
        $node.Value = $value
    }

    $fileXml.Save($filePath) 
} 

Function Log-Message {
    param (
        [string]$Message,
        [string]$Type = "INFO"
    )
    $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Type] $Message"
    Write-Host $logEntry
}

Function Update-AppSettingsConnectionStrings {
    param (
        [Parameter(Mandatory=$true)]
      [string]$databaseNameToUse,
        [Parameter(Mandatory=$true)]
        [string]$serverName,
     [Parameter(Mandatory=$true)]
      [string]$sourceDir
    )
    
    Write-Host "Updating appsettings*.json files with database name: $databaseNameToUse" -ForegroundColor Cyan
    
    # Build the connection string for environment variable
    $connectionString = "server=$serverName;database=$databaseNameToUse;Integrated Security=true;"
    
    # Set environment variable for current process
    $env:ConnectionStrings__SqlConnectionString = $connectionString
    Write-Host "Set process environment variable ConnectionStrings__SqlConnectionString: $connectionString" -ForegroundColor Cyan
    
    # Find all appsettings*.json files recursively
    $appSettingsFiles = Get-ChildItem -Path $sourceDir -Recurse -Filter "appsettings*.json"
    
    foreach ($file in $appSettingsFiles) {
        Write-Host "Processing file: $($file.FullName)" -ForegroundColor Gray
    
        $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
        
        # Check if ConnectionStrings section exists
        if ($content.PSObject.Properties.Name -contains "ConnectionStrings") {
            $connectionStringsObj = $content.ConnectionStrings

    # Update all connection strings that contain a database reference
     foreach ($property in $connectionStringsObj.PSObject.Properties) {
   $connectionString = $property.Value
   
       if ($connectionString -match "database=([^;]+)") {
# Replace the database name in the connection string
 $newConnectionString = $connectionString -replace "database=[^;]+", "database=$databaseNameToUse"
          
     # Also update server if needed
     $newConnectionString = $newConnectionString -replace "server=[^;]+", "server=$serverName"
      
            $connectionStringsObj.$($property.Name) = $newConnectionString
       Write-Host "  Updated $($property.Name): $newConnectionString" -ForegroundColor Green
    }
            }
       
            # Save the updated JSON
 $content | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
        }
    }
    
    Write-Host "Completed updating appsettings*.json files" -ForegroundColor Cyan
}
