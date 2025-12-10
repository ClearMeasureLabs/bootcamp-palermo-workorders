# Taken from psake https://github.com/psake

# Ensure SqlServer module is installed for Invoke-Sqlcmd
if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Write-Host "Installing SqlServer module..." -ForegroundColor DarkCyan
    try {
        # Register PSGallery if it's not registered
        if (-not (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue)) {
            Register-PSRepository -Default -ErrorAction Stop | Out-Null
        }
        # Trust PSGallery to avoid prompts
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
        Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser -Repository PSGallery -ErrorAction Stop
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
        $sqlPassword = Get-SqlServerPassword -ContainerName $containerName
        # $connectionString = "server=$serverName;database=$databaseNameToUse;User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
    }
    else {
        # $connectionString = "server=$serverName;database=$databaseNameToUse;Integrated Security=true;"
    }

    # use the existing connection string if it exists.
    $newConnectionString = $env:ConnectionStrings__SqlConnectionString;


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

                if ($oldConnectionString -match "database=([^;]+)") {
                    if ([string]::IsNullOrEmpty($newConnectionString)) {
                        # No connection string found, so let's create one
                        if (Test-IsLinux) {
                            $containerName = Get-ContainerName -DatabaseName $databaseNameToUse
                            $sqlPassword = Get-SqlServerPassword -ContainerName $containerName
                            $newConnectionString = "server=$serverName;database=$databaseNameToUse;User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
                        }
                        else {
                            # Replace the database name in the connection string
                            $newConnectionString = $oldConnectionString -replace "database=[^;]+", "database=$databaseNameToUse"
                    
                            # Also update server if needed
                            $newConnectionString = $newConnectionString -replace "server=[^;]+", "server=$serverName"
                        }
                    }
                    $connectionStringsObj.$($property.Name) = $newConnectionString
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

Function Test-IsLocalBuild {
    <#
    .SYNOPSIS
        Tests if the current script is running locally (not in CI/CD)
    .DESCRIPTION
        Returns true if not running in GitHub Actions or Azure DevOps
    .OUTPUTS
        [bool] True if running locally, False if in CI/CD
    #>
    
    return -not ((Test-IsGitHubActions) -or (Test-IsAzureDevOps))
}


Function New-SqlServerDatabase {
    param (
        [Parameter(Mandatory = $true)]
        [string]$serverName,
        [Parameter(Mandatory = $true)]
        [string]$databaseName,
        [Parameter(Mandatory = $true)]
        [string]$user ,
        [Parameter(Mandatory = $true)]
        [string]$password
    )

    $saCred = New-object System.Management.Automation.PSCredential($user, (ConvertTo-SecureString -String $password -AsPlainText -Force))
    
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
        if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
            Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $dropDbCmd -Encrypt Optional -TrustServerCertificate
            Invoke-Sqlcmd -ServerInstance $serverName -Database master -Credential $saCred -Query $createDbCmd -Encrypt Optional -TrustServerCertificate
        } else {
            # Fallback to docker exec if Invoke-Sqlcmd is not available
            # Using -i for interactive mode to avoid password in command line
            $dropDbCmdEscaped = $dropDbCmd -replace '"', '\"' -replace "`r`n", " " -replace "`n", " "
            $createDbCmdEscaped = $createDbCmd -replace '"', '\"' -replace "`r`n", " " -replace "`n", " "
            
            # Set password as environment variable for the container to avoid exposing in process list
            docker exec -e "SQLCMDPASSWORD=$sqlPassword" $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -d master -Q "$dropDbCmdEscaped" -C 2>&1 | Out-Null
            docker exec -e "SQLCMDPASSWORD=$sqlPassword" $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -d master -Q "$createDbCmdEscaped" -C 2>&1 | Out-Null
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
    $containersOnPort1433 = docker ps --filter "publish=1433" --format "{{.Names}}"
    
    foreach ($container in $containersOnPort1433) {
        if ($container) {
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
    $sqlPassword = Get-SqlServerPassword -ContainerName $containerName
    
    # Create new container
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$sqlPassword" -p 1433:1433 --name $containerName -d $imageName 
    Log-Message -Message "Waiting for SQL Server to be ready..." -Type "INFO"
    
    $maxWaitSeconds = 120
    $waitIntervalSeconds = 3
    $elapsedSeconds = 0
    $isReady = $false
    while ($elapsedSeconds -lt $maxWaitSeconds) {
        try {
            # Try using docker exec as an alternative to Invoke-Sqlcmd if the module is not available
            if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
                Invoke-Sqlcmd -ServerInstance "localhost,1433" -Username "sa" -Password $sqlPassword -Query "SELECT 1" -Encrypt Optional -TrustServerCertificate -ErrorAction Stop | Out-Null
            } else {
                # Fallback to docker exec if Invoke-Sqlcmd is not available
                # Use environment variable to avoid password in command line
                $result = docker exec -e "SQLCMDPASSWORD=$sqlPassword" $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -Q "SELECT 1" -C 2>&1
                if ($LASTEXITCODE -ne 0) {
                    throw "SQL Server not ready yet"
                }
            }
            $isReady = $true
            break
        } catch {
            if ($elapsedSeconds % 30 -eq 0) {
                Log-Message -Message "Still waiting for SQL Server... Error: $_" -Type "WARNING"
            }
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

Function Get-SqlServerPassword {
    <#
    .SYNOPSIS
        Generates SQL Server password for Docker containers. This is for testing/CI purposes only. 
    .DESCRIPTION
        Creates a SQL Server password based on the container name that meets complexity requirements.
        Password must be at least 8 characters with uppercase, lowercase, digit, and symbol.
    .PARAMETER ContainerName
        The name of the Docker container to generate password for
    .OUTPUTS
        [string] A password that meets SQL Server complexity requirements
    .EXAMPLE
        Get-SqlServerPassword -ContainerName "mydb-mssql"
        Returns: "mydb-mssql#1A"
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$ContainerName
    )
    
    return "${ContainerName}#1A"
}

Function Test-IsOllamaRunning {
    <#
    .SYNOPSIS
        Tests if Ollama is installed and running
    .DESCRIPTION
        Checks if Ollama is installed and the Ollama service is accessible by testing the API endpoint
    .PARAMETER LogOutput
        If true, outputs details about the Ollama installation (if any)
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
            Log-Message -Message "Ollama is not installed or not in PATH! Install Ollama from https://ollama.ai/download" -Type "WARNING"
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

<#
.SYNOPSIS
    Joins multiple path segments into a single path using nested Join-Path calls.
.DESCRIPTION
    Creates a cross-platform and cross-version compatible path by joining multiple segments.
    Works with PowerShell 5.1+ and pwsh 6.0+. Handles proper path separators for Windows and Linux.
.PARAMETER PathSegments
    Array of path segments to join together.
.EXAMPLE
    Join-PathSegments "C:\test", "Database", "bin", "Release", "net9.0", "test.dll"
    Returns: C:\test\Database\bin\Release\net9.0\test.dll (on Windows)
.EXAMPLE
    Join-PathSegments "/home", "user", "projects", "src", "file.txt"
    Returns: /home/user/projects/src/file.txt (on Linux)
#>
function Join-PathSegments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$PathSegments
    )
    
    if ($PathSegments.Count -eq 0) {
        throw "At least one path segment must be provided"
    }
    
    if ($PathSegments.Count -eq 1) {
        return $PathSegments[0]
    }
    
    $result = $PathSegments[0]
    for ($i = 1; $i -lt $PathSegments.Count; $i++) {
        $result = Join-Path $result $PathSegments[$i]
    }
    
    return $result
}

Function Get-ConnectionStringComponents {
    <#
    .SYNOPSIS
        Extracts database connection string components from ConnectionStrings__SqlConnectionString environment variable
    .DESCRIPTION
        Parses the connection string and extracts the database server, database name, database user, and database password.
        Supports various connection string formats including Server/Data Source, Database/Initial Catalog, User ID/User, and Password.
    .OUTPUTS
        [PSCustomObject] An object with properties: Server, Database, User, Password
    .EXAMPLE
        $env:ConnectionStrings__SqlConnectionString = "Server=localhost;Database=mydb;User ID=sa;Password=secret123;"
        $components = Get-ConnectionStringComponents
        Returns: @{ Server = "localhost"; Database = "mydb"; User = "sa"; Password = "secret123" }
    .EXAMPLE
        $env:ConnectionStrings__SqlConnectionString = "Data Source=server1;Initial Catalog=ChurchBulletin;User=admin;Password=pass123;"
        $components = Get-ConnectionStringComponents
        Returns: @{ Server = "server1"; Database = "ChurchBulletin"; User = "admin"; Password = "pass123" }
    #>
    param (
        [Parameter(Mandatory = $false)]
        [string]$ConnectionString = $env:ConnectionStrings__SqlConnectionString
    )

    if ([string]::IsNullOrEmpty($ConnectionString)) {
        throw "ConnectionStrings__SqlConnectionString environment variable is not set and no connection string was provided"
    }

    # Extract server (supports both "Server=" and "Data Source=")
    $server = $null
    if ($ConnectionString -match "(?:Server|Data Source)=([^;]+)") {
        $server = $matches[1].Trim()
    }

    # Extract database (supports both "Database=" and "Initial Catalog=")
    $database = $null
    if ($ConnectionString -match "(?:Database|Initial Catalog)=([^;]+)") {
        $database = $matches[1].Trim()
    }

    # Extract user (supports both "User ID=" and "User=")
    $user = $null
    if ($ConnectionString -match "(?:User ID|User)=([^;]+)") {
        $user = $matches[1].Trim()
    }

    # Extract password
    $password = $null
    if ($ConnectionString -match "Password=([^;]+)") {
        $password = $matches[1].Trim()
    }

    return [PSCustomObject]@{
        Server   = $server
        Database = $database
        User     = $user
        Password = $password
    }
}


