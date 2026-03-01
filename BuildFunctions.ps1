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

    # Suppress DEBUG messages unless BUILD_LOG_LEVEL is set to DEBUG
    if ($Type -eq "DEBUG" -and $env:BUILD_LOG_LEVEL -ne "DEBUG") {
        return
    }

    $color = switch ($Type) {
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "INFO" { "Cyan" }
        "DEBUG" { "DarkGray" }
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
        [string]$databaseName
    )

    $containerName = Get-ContainerName -DatabaseName $databaseName
    $sqlPassword = Get-SqlServerPassword -ContainerName $containerName
    $saCred = New-object System.Management.Automation.PSCredential("sa", (ConvertTo-SecureString -String $sqlPassword -AsPlainText -Force))
    
    $dropDbCmd = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$databaseName')
BEGIN
    ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$databaseName];
END
"@

    $createDbCmd = "CREATE DATABASE [$databaseName];"
	Log-Message "Creating SQL Server in Docker for integration tests for $databaseName on $serverName" -Type "DEBUG"

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

    Log-Message -Message "Recreated database '$databaseName' on server '$serverName'" -Type "DEBUG"
}

Function New-DockerContainerForSqlServer {
    param (
        [Parameter(Mandatory = $true)]
        [string]$containerName
    )

    $imageName = "mcr.microsoft.com/mssql/server:2022-latest"

    # Stop any containers using port 1433
    Log-Message -Message "Checking for containers using port 1433..." -Type "DEBUG"
    $containersOnPort1433 = docker ps --filter "publish=1433" --format "{{.Names}}"
    
    foreach ($container in $containersOnPort1433) {
        if ($container) {
            Log-Message -Message "Stopping container '$container' that is using port 1433..." -Type "DEBUG"
            docker stop $container | Out-Null
            docker rm $container | Out-Null
        }
    }

    # Check if our specific container exists (running or stopped)
    $existingContainer = docker ps -a --filter "name=^${containerName}$" --format "{{.Names}}"
    if ($existingContainer) {
        Log-Message -Message "Removing existing container '$containerName'..." -Type "DEBUG"
        docker rm -f $existingContainer | Out-Null
    }
    
    # Create SQL Server password that meets complexity requirements
    # Must be at least 8 characters with uppercase, lowercase, digit, and symbol
    $sqlPassword = Get-SqlServerPassword -ContainerName $containerName
    
    # Create new container
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$sqlPassword" -p 1433:1433 --name $containerName -d $imageName 
    Log-Message -Message "Waiting for SQL Server to be ready..." -Type "DEBUG"
    
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
    
    Log-Message -Message "SQL Server Docker container '$containerName' should be running." -Type "DEBUG"

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
            Log-Message -Message "Install Docker from: https://docs.docker.com/engine/install/" -Type "DEBUG"
        }
        return $false
    }
    else {
        if ($LogOutput) {
            Log-Message -Message "Docker found at: $dockerPath" -Type "DEBUG"
        }
        
        # Check if Docker daemon is running
        try {
            $dockerVersion = & docker version --format "{{.Server.Version}}" 2>$null
            if ($dockerVersion) {
                if ($LogOutput) {
                    Log-Message -Message "Docker daemon is running (version: $dockerVersion)" -Type "DEBUG"
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
     
        Log-Message -Message "Generated unique database name: $uniqueName" -Type "DEBUG"
        return $uniqueName
    }
    else {
        Log-Message -Message "Using base database name: $baseName" -Type "DEBUG"
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
