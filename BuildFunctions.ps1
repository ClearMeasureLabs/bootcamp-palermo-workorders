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

Function Get-OSPlatform {
    $os = $PSVersionTable.OS
    if ($os -match "Windows") {
        return "Windows"
    }
    elseif ($os -match "Linux") {
        return "Linux"
    }
    elseif ($os -match "Darwin") {
        return "macOS"
    }
    else {
        return "Unknown"
    }
}

Function New-SqlServerDatabase {
    param (
        [Parameter(Mandatory=$true)]
        [string]$serverName,
        [Parameter(Mandatory=$true)]
        [string]$databaseName
    )

    $saCred = New-object System.Management.Automation.PSCredential("sa", (ConvertTo-SecureString -String $databaseName -AsPlainText -Force))
    
    $dropDbCmd = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$databaseName')
BEGIN
    ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$databaseName];
END
"@

    $createDbCmd = "CREATE DATABASE [$databaseName];"

    try 
    {
        Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $dropDbCmd -Encrypt Optional -TrustServerCertificate
         Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $createDbCmd -Encrypt Optional -TrustServerCertificate
    } 
    catch {
        Log-Message -Message "Error creating database '$databaseName' on server '$serverName': $_" -Type "ERROR"
        throw $_
    }

    Log-Message -Message "Recreated database '$databaseName' on server '$serverName'" -Type "INFO"
}




Function New-DockerSqlServer {
    param (
        [Parameter(Mandatory=$true)]
        [string]$databaseName
    )

    $containerName = "sql2022-bootcamp-tests-$databaseName"
    $imageName = "mcr.microsoft.com/mssql/server:2022-latest"

    # Stop any containers using port 1433
    Log-Message -Message "Checking for containers using port 1433..." -Type "INFO"
    $containersOnPort1433 = docker ps --format "table {{.Names}}\t{{.Ports}}" | Select-String ":1433->" | ForEach-Object { 
        ($_ -split '\s+')[0] 
    }
    
    foreach ($container in $containersOnPort1433) {
        if ($container -and $container -ne "NAMES") {
            Log-Message -Message "Stopping container '$container' that is using port 1433..." -Type "INFO"
            docker stop $container | Out-Null
            docker rm $container | Out-Null
        }
    }

    # Check if our specific container exists
    $containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"
    if (-not $containerStatus) {
        docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$databaseName" -p 1433:1433 --name $containerName -d $imageName | Out-Null
        Start-Sleep -Seconds 10
    }
    Log-Message -Message "SQL Server Docker container '$containerName' is running." -Type "INFO"

}

