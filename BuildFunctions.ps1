# Taken from psake https://github.com/psake/psake

# Ensure SqlServer module is installed for Invoke-Sqlcmd
if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Write-Host "Installing SqlServer module..." -ForegroundColor DarkCyan
    try {
        Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser -ErrorAction Stop
        Write-Host "SqlServer module installed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to install SqlServer module: $_" -ForegroundColor Red
        Write-Host "Some database operations may not work without this module" -ForegroundColor Yellow
    }
}

try {
    Import-Module SqlServer -ErrorAction Stop
}
catch {
    Write-Host "Warning: Could not import SqlServer module. Invoke-Sqlcmd will not be available." -ForegroundColor Yellow
}

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

Function Get-RedactedConnectionString {
    <#
    .SYNOPSIS
        Returns a connection string with the password redacted
    .DESCRIPTION
        Takes a connection string and replaces the password value with ***
    .PARAMETER ConnectionString
        The connection string to redact
    .OUTPUTS
        [string] The connection string with password replaced by ***
    .EXAMPLE
        Get-RedactedConnectionString -ConnectionString "Server=localhost;Database=mydb;Password=secret123;User=sa"
        Returns: "Server=localhost;Database=mydb;Password=***;User=sa"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString
    )
    
    return $ConnectionString -replace "Password=[^;]*", "Password=***"
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

    Log-Message "Updating appsettings*.json files with database name: $databaseNameToUse" -Type "INFO"

    # Build the connection string for environment variable
    if (Test-IsLinux) {
        $containerName = Get-ContainerName -DatabaseName $databaseNameToUse
        $sqlPassword = "${containerName}#1A"
        $connectionString = "server=$serverName;database=$databaseNameToUse;User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
    }
    else {
        $connectionString = "server=$serverName;database=$databaseNameToUse;Integrated Security=true;"
    }


    # Set environment variable for current process
    $env:ConnectionStrings__SqlConnectionString = $connectionString
    Log-Message "Set process environment variable ConnectionStrings__SqlConnectionString: $(Get-RedactedConnectionString -ConnectionString $connectionString)" -Type "INFO"

    # Find all appsettings*.json files recursively
    $appSettingsFiles = Get-ChildItem -Path $sourceDir -Recurse -Filter "appsettings*.json"
    
    foreach ($file in $appSettingsFiles) {
        Log-Message "Processing file: $($file.FullName)" -Type "INFO"
    
        $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
        
        # Check if ConnectionStrings section exists
        if ($content.PSObject.Properties.Name -contains "ConnectionStrings") {
            $connectionStringsObj = $content.ConnectionStrings

            # Update all connection strings that contain a database reference
            foreach ($property in $connectionStringsObj.PSObject.Properties) {
                $oldConnectionString = $property.Value

                Log-Message "Found connection string $($property.Name) : $(Get-RedactedConnectionString -ConnectionString $oldConnectionString)" -Type "INFO"
                if ($oldConnectionString -match "database=([^;]+)") {

                    if (Test-IsLinux) {
                        $containerName = Get-ContainerName -DatabaseName $databaseNameToUse
                        $sqlPassword = "${containerName}#1A"
                        $newConnectionString = "server=$serverName;database=$databaseNameToUse;User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
                    }
                    else {
                        # Replace the database name in the connection string
                        $newConnectionString = $oldConnectionString -replace "database=[^;]+", "database=$databaseNameToUse"
                
                        # Also update server if needed
                        $newConnectionString = $newConnectionString -replace "server=[^;]+", "server=$serverName"
                    }
        
                    $connectionStringsObj.$($property.Name) = $newConnectionString
                    Log-Message "Updated $($property.Name): $(Get-RedactedConnectionString -ConnectionString $newConnectionString)" -Type "INFO"
                }
            }
       
            # Save the updated JSON
            $content | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
        }
    }
    
    Log-Message "Completed updating appsettings*.json files" -Type "INFO"
}



Function Get-OSPlatform {
    # In PowerShell Core 6+, use built-in variables
    if ($null -ne $IsWindows) {
        if ($IsWindows) { return "Windows" }
        if ($IsLinux) { return "Linux" }
        if ($IsMacOS) { return "macOS" }
    }
    
    # Fallback for Windows PowerShell 5.1 (which only runs on Windows)
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        return "Windows"
    }
    
    # Additional fallback using environment
    if ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) {
        return "Linux"
    }
    
    return "Windows"
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
    # PowerShell Core 6+ has $IsLinux variable
    if ($null -ne $IsLinux) { 
        return $IsLinux
    }
    
    # Windows PowerShell 5.1 only runs on Windows
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        return $false
    }
    
    # Fallback check
    return (Get-OSPlatform -eq "Linux")
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
    if ($IsWindows) { 
        return $true
    }
    
    if (Get-OSPlatform -match "Windows") {
        return $true
    }

    return $false
}

Function Test-IsGitHubActions {
    <#
    .SYNOPSIS
        Tests if the current script is running in GitHub Actions
    .DESCRIPTION
        Returns true if the current PowerShell session is running within a GitHub Actions workflow
    .OUTPUTS
        [bool] True if running in GitHub Actions, False otherwise
    .EXAMPLE
        if (Test-IsGitHubActions) {
            Write-Host "Running in GitHub Actions"
        }
    #>
    # GitHub Actions sets the GITHUB_ACTIONS environment variable to 'true'
    $githubActions = $env:GITHUB_ACTIONS
    
    if ($githubActions -eq 'true') {
        return $true
    }
    
    # Additional check for GITHUB_WORKFLOW which is also set by GitHub Actions
    if (-not [string]::IsNullOrEmpty($env:GITHUB_WORKFLOW)) {
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

    $containerName = Get-ContainerName -DatabaseName $databaseName
    $sqlPassword = "${containerName}#1A"
    $saCred = New-object System.Management.Automation.PSCredential("sa", (ConvertTo-SecureString -String $sqlPassword -AsPlainText -Force))
    
    $dropDbCmd = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$databaseName')
BEGIN
    ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$databaseName];
END
"@

    $createDbCmd = "CREATE DATABASE [$databaseName];"
	Log-Message "Creating SQL Server in Docker for integration tests for $databaseName on $serverName" -Type "INFO"

    try {
        # Try using Invoke-Sqlcmd if available, otherwise use docker exec
        if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
            Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $dropDbCmd -Encrypt Optional -TrustServerCertificate
            Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $createDbCmd -Encrypt Optional -TrustServerCertificate
        } else {
            # Fallback: Use docker exec to run sqlcmd inside the container
            docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -d master -q $dropDbCmd -C | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to drop database '$databaseName' in container '$containerName' using docker exec"
            }
            docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -d master -Q $createDbCmd -C | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create database '$databaseName' in container '$containerName' using docker exec"
            }
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
        [string]$containerName
    )

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

    # Check if our specific container exists (running or stopped)
    $existingContainer = docker ps -a --filter "name=^${containerName}$" --format "{{.Names}}"
    if ($existingContainer) {
        Log-Message -Message "Removing existing container '$containerName'..." -Type "INFO"
        docker rm -f $existingContainer | Out-Null
    }
    
    # Create SQL Server password that meets complexity requirements
    # Must be at least 8 characters with uppercase, lowercase, digit, and symbol
    $sqlPassword = "${containerName}#1A"
    
    # Create new container
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$sqlPassword" -p 1433:1433 --name $containerName -d $imageName 
    Log-Message -Message "Waiting for SQL Server to be ready..." -Type "INFO"
    
    $maxWaitSeconds = 120
    $waitIntervalSeconds = 3
    $elapsedSeconds = 0
    $isReady = $false
    while ($elapsedSeconds -lt $maxWaitSeconds) {
        try {
            # Try using Invoke-Sqlcmd if available, otherwise use docker exec
            if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
                Invoke-Sqlcmd -ServerInstance "localhost,1433" -Username "sa" -Password $sqlPassword -Query "SELECT 1" -Encrypt Optional -TrustServerCertificate -ErrorAction Stop | Out-Null
            } else {
                # Fallback: Use docker exec to run sqlcmd inside the container
                $result = docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPassword -Q "SELECT 1" -C 2>&1
                if ($LASTEXITCODE -ne 0) {
                    throw "Docker exec failed"
                }
            }
            $isReady = $true
            break
        } catch {
            Start-Sleep -Seconds $waitIntervalSeconds
            $elapsedSeconds += $waitIntervalSeconds
        }
    }
    if (-not $isReady) {
        Log-Message -Message "SQL Server did not become ready within $maxWaitSeconds seconds." -Type "ERROR"
        throw "SQL Server Docker container '$containerName' did not become ready in time."
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
        [Parameter(Mandatory = $false)]
        [bool]$LogOutput = $false
    )
    
    $dockerPath = (Get-Command docker -ErrorAction SilentlyContinue).Source
    if (-not $dockerPath) {
        if ($LogOutput) {
            Log-Message -Message "Docker is not installed or not in PATH" -Type "ERROR"
            Log-Message -Message "Install Docker from: https://docs.docker.com/engine/install/" -Type "INFO"
        }
        return $false
    }
    else {
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
            }
            else {
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
        [string]$baseName,
        
        [Parameter(Mandatory = $false)]
        [bool]$generateUnique = $false
    )
    
    if ($generateUnique) {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        $randomChars = -join ((65..90) + (97..122) | Get-Random -Count 4 | ForEach-Object { [char]$_ })
        $uniqueName = "${baseName}_${timestamp}_${randomChars}"
     
        Log-Message -Message "Generated unique database name: $uniqueName" -Type "INFO"
        return $uniqueName
    }
    else {
        Log-Message -Message "Using base database name: $baseName" -Type "INFO"
        return $baseName
    }
}

Function Get-ContainerName {
    <#
    .SYNOPSIS
        Creates a container name from a database name
    .DESCRIPTION
        Takes a database name and returns a container name in the format
    .PARAMETER DatabaseName
        The database name to create the container name from
    .OUTPUTS
        [string] A unique container name based on the database name
    .EXAMPLE
        Get-ContainerName -DatabaseName "MyTestDB"
        Returns: "mytestdb-mssql"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$DatabaseName
    )
    
    return "$DatabaseName-mssql".ToLower()
}

Function Test-IsOllamaRunning {
    <#
    .SYNOPSIS
        Tests if Ollama is installed and running
    .DESCRIPTION
        Checks if Ollama is installed and the Ollama service is accessible by testing the API endpoint
    .PARAMETER LogOutput
        If true, outputs detailed logging information
    .PARAMETER OllamaUrl
        The URL of the Ollama service. Defaults to http://localhost:11434
    .OUTPUTS
        [bool] True if Ollama is running and accessible, False otherwise
    .EXAMPLE
        if (Test-IsOllamaRunning -LogOutput $true) {
            Write-Host "Ollama is running"
        }
    #>
    param (
        [Parameter(Mandatory = $false)]
        [bool]$LogOutput = $false,
        
        [Parameter(Mandatory = $false)]
        [string]$OllamaUrl = "http://localhost:11434"
    )
    
    # Check if Ollama CLI is installed
    $ollamaPath = (Get-Command ollama -ErrorAction SilentlyContinue).Source
    if (-not $ollamaPath) {
        if ($LogOutput) {
            Log-Message -Message "Ollama is not installed or not in PATH" -Type "ERROR"
            Log-Message -Message "Install Ollama from: https://ollama.ai/download" -Type "INFO"
        }
        return $false
    }
    else {
        if ($LogOutput) {
            Log-Message -Message "Ollama found at: $ollamaPath" -Type "INFO"
        }
    }
    
    # Check if Ollama service is running by testing the API
    try {
        $response = Invoke-WebRequest -Uri "$OllamaUrl/api/tags" -Method Get -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            if ($LogOutput) {
                $content = $response.Content | ConvertFrom-Json
                $modelCount = ($content.models | Measure-Object).Count
                Log-Message -Message "Ollama service is running at $OllamaUrl" -Type "INFO"
                Log-Message -Message "Available models: $modelCount" -Type "INFO"
            }
            return $true
        }
        else {
            if ($LogOutput) {
                Log-Message -Message "Ollama service returned unexpected status code: $($response.StatusCode)" -Type "ERROR"
            }
            return $false
        }
    }
    catch {
        if ($LogOutput) {
            Log-Message -Message "Ollama service is not accessible at $OllamaUrl" -Type "ERROR"
            Log-Message -Message "Error: $_" -Type "ERROR"
            if (Test-IsWindows) {
                Log-Message -Message "Try starting Ollama from the Start Menu or run: ollama serve" -Type "INFO"
            }
            elseif (Test-IsLinux) {
                Log-Message -Message "Try starting Ollama: sudo systemctl start ollama" -Type "INFO"
            }
            else {
                Log-Message -Message "Try starting Ollama: ollama serve" -Type "INFO"
            }
        }
        return $false
    }
}


