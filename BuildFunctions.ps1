# Taken from psake https://github.com/psake/psake

# Ensure SqlServer module is installed for Invoke-Sqlcmd
if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Log-Message "Installing SqlServer module..." -Type "INFO"
    Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
}
Import-Module SqlServer -ErrorAction SilentlyContinue

<#
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = 1)][scriptblock]$cmd,
        [Parameter(Position = 1, Mandatory = 0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
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

    $color = switch ($Type) {
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }

    $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Type] $Message"
    Write-Host $logEntry -ForegroundColor $color
}

Function Update-AppSettingsConnectionStrings {
    param (
        [Parameter(Mandatory = $true)]
        [string]$databaseNameToUse,
        [Parameter(Mandatory = $true)]
        [string]$serverName,
        [Parameter(Mandatory = $true)]
        [string]$sourceDir
    )
    
    Write-Host "Updating appsettings*.json files with database name: $databaseNameToUse" -ForegroundColor Cyan
    
    if (Test-IsLinux) 
    {
        $connectionString = "Data Source=$serverName;Initial Catalog=$databaseNameToUse;User ID=sa;Password=$databaseNameToUse;TrustServerCertificate=true;Integrated Security=false;Encrypt=false"
    }
    else
    {
        # [TO20251112] This is the connection string format for LocalDB
        $connectionString = "server=$serverName;database=$databaseNameToUse;Integrated Security=true;"
    }


    # Set environment variable for current process
    $env:ConnectionStrings__SqlConnectionString = $connectionString
    $redactedConnectionString = $oldConnectionString -replace "Password=[^;]*", "Password=***"

    Write-Host "Set process environment variable ConnectionStrings__SqlConnectionString: $redactedConnectionString" -ForegroundColor Cyan
    
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
                $oldConnectionString = $property.Value
                $redactedConnectionString = $oldConnectionString -replace "Password=[^;]*", "Password=***"

                Write-Host "  Found connection string $($property.Name) : $redactedConnectionString" -ForegroundColor Yellow
                if ($oldConnectionString -match "database=([^;]+)") {

                    # Replace the database name in the connection string
                    #$newConnectionString = $connectionString -replace "database=[^;]+", "database=$databaseNameToUse"
            
                    # Also update server if needed
                    #$newConnectionString = $newConnectionString -replace "server=[^;]+", "server=$serverName"
        
                    $connectionStringsObj.$($property.Name) = $connectionString
                    # Redact password from output for security
                    Write-Host "  Updated $($property.Name): $redactedConnectionString" -ForegroundColor Green
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

    return "Unknown"
}

Function Test-IsLinux {
    <#
    .SYNOPSIS
        Tests if the current script is running on Linux
    .DESCRIPTION
        Returns true if the current PowerShell session is running on a Linux operating system
    .OUTPUTS
        [bool] True if running on Linux, False otherwise
    #>
    
    if (Get-OSPlatform -match "Linux") {
        return $true
    }
    if ($IsLinux) { 
        return $true
    }

    return $false
}

Function Test-IsWindows {
    <#
    .SYNOPSIS
        Tests if the current script is running on Windows
    .DESCRIPTION
        Returns true if the current PowerShell session is running on a Windows operating system
    .OUTPUTS
        [bool] True if running on Windows, False otherwise
    #>
    
    if (Get-OSPlatform -match "Windows") {
        return $true
    }
    if ($IsWindows) { 
        return $true
    }

    return $false
}

Function Test-IsAzureDevOps {
    <#
    .SYNOPSIS
        Tests if the current script is running in Azure DevOps
    .DESCRIPTION
        Returns true if the current PowerShell session is running within an Azure DevOps pipeline
    .OUTPUTS
        [bool] True if running in Azure DevOps, False otherwise
    #>
    
    if ($env:TF_BUILD -eq "True") {
        return $true
    }

    return $false
}


Function New-SqlServerDatabase {
    param (
        [Parameter(Mandatory = $true)]
        [string]$serverName,
        [Parameter(Mandatory = $true)]
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

    try {
        Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $dropDbCmd -Encrypt Optional -TrustServerCertificate
        Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $createDbCmd -Encrypt Optional -TrustServerCertificate
    } 
    catch {
        Log-Message -Message "Error creating database '$databaseName' on server '$serverName': $_" -Type "ERROR"
        throw $_
    }

    Log-Message -Message "Recreated database '$databaseName' on server '$serverName'" -Type "INFO"
}

Function New-DockerContainerForSqlServer {
    param (
        [Parameter(Mandatory = $true)]
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
        docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$databaseName" -p 1433:1433 --name $containerName -d $imageName 
        Start-Sleep -Seconds 10
    }
    Log-Message -Message "SQL Server Docker container '$containerName' should be running." -Type "INFO"

}

Function Test-IsDockerRunning {
    <#
    .SYNOPSIS
        Tests if Docker is installed and running
    .DESCRIPTION
        Checks if Docker is installed and the Docker daemon is accessible
    .PARAMETER LogOutput
        If true, outputs detailed logging information
    .OUTPUTS
        [bool] True if Docker is running, False otherwise
    #>
    param (
        [Parameter(Mandatory=$false)]
        [bool]$LogOutput = $false
    )
    
    $dockerPath = (Get-Command docker -ErrorAction SilentlyContinue).Source
    if (-not $dockerPath) {
        if ($LogOutput) {
            Log-Message -Message "Docker is not installed or not in PATH" -Type "ERROR"
            Log-Message -Message "Install Docker from: https://docs.docker.com/engine/install/" -Type "INFO"
        }
        return $false
    } else {
        if ($LogOutput) {
            Log-Message -Message "Docker found at: $dockerPath" -Type "INFO"
        }
        
        # Check if Docker daemon is running
        try {
            $dockerVersion = & docker version --format "{{.Server.Version}}" 2>$null
            if ($dockerVersion) {
                if ($LogOutput) {
                    Log-Message -Message "Docker daemon is running (version: $dockerVersion)" -Type "INFO"
                }
            } else {
                if ($LogOutput) {
                    Log-Message -Message "Docker is installed but the daemon may not be running. Try: sudo systemctl start docker" -Type "ERROR"
                }
                return $false
            }
        }
        catch {
            if ($LogOutput) {
                Log-Message -Message "Docker is installed but the daemon is not accessible. Try: sudo systemctl start docker" -Type "ERROR"
            }
            return $false   
        }
    }

    return $true
}

Function Generate-UniqueDatabaseName {
	param (
		[Parameter(Mandatory = $true)]
		[string]$baseName
	)
    
	$timestamp = Get-Date -Format "yyyyMMddHHmmss"
	$randomChars = -join ((65..90) + (97..122) | Get-Random -Count 4 | ForEach-Object { [char]$_ })
	$uniqueName = "${baseName}_${timestamp}_${randomChars}"
 
	Log-Message -Message "Generated unique database name: $uniqueName" -Type "INFO"
	return $uniqueName
}