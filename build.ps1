. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString)
{
    Write-Host "Clearing ConnectionStrings__SqlConnectionString environment variable"
    $env:ConnectionStrings__SqlConnectionString = $null
    [Environment]::SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", $null, "User")
}

$projectName = "ChurchBulletin"
$base_dir = resolve-path .\
$source_dir = Join-Path $base_dir "src"
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath = Join-Path $source_dir "UI" "Server"
$databaseProjectPath = Join-Path $source_dir "Database"
$dbProjectName = Join-Path $databaseProjectPath "Database.csproj"
$projectConfig = $env:BuildConfiguration
$framework = "net9.0"
$version = $env:BUILD_BUILDNUMBER
$script:databaseScripts = Join-Path $source_dir "Database" "scripts"

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"
$solutionName = Join-Path $source_dir "$projectName.sln"

$databaseAction = $env:DatabaseAction
if ( [string]::IsNullOrEmpty($databaseAction))
{
    $databaseAction = "Rebuild"
}
$script:databaseName = $projectName
if ( [string]::IsNullOrEmpty($script:databaseName))
{
    $script:databaseName = $projectName
}

if ( [string]::IsNullOrEmpty($version))
{
    $version = "1.0.0"
}
if ( [string]::IsNullOrEmpty($projectConfig))
{
    $projectConfig = "Release"
}

$script:databaseServer = $env:DatabaseServer
$script:databaseInDocker = $false;
if ([string]::IsNullOrEmpty($script:databaseServer))
{
    if (Test-IsLinux)
    {
        $script:databaseInDocker = $true;
        $script:databaseServer = "localhost"
        Log-Message "Linux detected. No database server specified in environment variable 'DatabaseServer'. Using localhost."
    }
    else
    {
        Log-Message "Windows detected. No database server specified in environment variable 'DatabaseServer'. Using LocalDB instance."
        $script:databaseServer = "(LocalDb)\MSSQLLocalDB"
    }
}
else
{
    Log-Message "Using database server from environment variable 'DatabaseServer': $script:databaseServer"
}


Function Init
{
    Log-Message "Project Configuration: $projectConfig. Version: $version. Database server: $script:databaseServer. Database name: $script:databaseName" -Type "INFO"

    # Check for PowerShell 7
    $pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
    if (-not $pwshPath)
    {
        Log-Message "PowerShell is not installed. Please install it from https://aka.ms/powershell" -Type "ERROR"
    }
    else
    {
        Log-Message "PowerShell found at: $pwshPath" -Type "INFO"
    }

    if (Test-Path "build")
    {
        Remove-Item -Path "build" -Recurse -Force
    }
    New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

    exec { & dotnet clean $( Join-Path $source_dir "$projectName.sln" ) -nologo -v $verbosity }
    exec { & dotnet restore $( Join-Path $source_dir "$projectName.sln" ) -nologo --interactive -v $verbosity }

}

Function Compile
{
    exec {
        & dotnet build $( Join-Path $source_dir "$projectName.sln" ) -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="Church Bulletin"
    }
}

Function UnitTests
{
    Push-Location -Path $unitTestProjectPath

    try
    {
        exec {
            & dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $( Join-Path $test_dir "UnitTests" ) --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
        }
    }
    finally
    {
        Pop-Location
    }
}

Function IntegrationTest
{
    Push-Location -Path $integrationTestProjectPath

    try
    {
        exec {
            & dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $( Join-Path $test_dir "IntegrationTests" ) --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
        }
    }
    finally
    {
        Pop-Location
    }
}

Function AcceptanceTests
{
    $projectConfig = "Debug"
    Push-Location -Path $acceptanceTestProjectPath

    pwsh (Join-Path "bin" "Debug" $framework "playwright.ps1") install --with-deps

    try
    {
        exec {
            & dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $( Join-Path $test_dir "AcceptanceTests" ) --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
        }
    }
    finally
    {
        Pop-Location
    }
}

Function MigrateDatabaseLocal
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$databaseServerFunc,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$databaseNameFunc
    )

    $dbUser = ""
    $dbPwd = ""
    if ($script:databaseInDocker)
    {
        $dbPwd = $script:databaseName
        $dbUser = "sa"
        
        Log-Message -Message "Setting up SQL Server in Docker" -Type "INFO"
        if (Test-IsDockerRunning -LogOutput $true)
        {
            Log-Message -Message "Standing up SQL Server in Docker." -Type "INFO"
            New-DockerContainerForSqlServer -containerName $script:databaseName
            New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
        }
        else
        {
            Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
            throw "Docker is not running."
        }

    }
    $databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
    exec { & dotnet $databaseDll $script:databaseAction $databaseServerFunc $databaseNameFunc $script:databaseScripts $dbUser $dbPwd}
}

Function Create-SqlServerInDocker
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$serverName,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$dbAction,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$scriptDir
    )

    New-DockerContainerForSqlServer $script:databaseName
    New-SqlServerDatabase -serverName $serverName -databaseName $script:databaseName

    $databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
    exec {
        & dotnet $databaseDll $dbAction $serverName $script:databaseName $scriptDir "sa" $script:databaseName 
    }
}

Function PackageUI
{
    exec {
        & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
    exec {
        & dotnet-octo pack --id "$projectName.UI" --version $version --basePath $( Join-Path $uiProjectPath "bin" $projectConfig $framework "publish" ) --outFolder $build_dir  --overwrite
    }
}

Function PackageDatabase
{
    exec {
        & dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
    }
}

Function PackageAcceptanceTests
{
    # Use Debug configuration so full symbols are available to display better error messages in test failures
    exec {
        & dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
    }
    exec {
        & dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $( Join-Path $acceptanceTestProjectPath "bin" "Debug" $framework "publish" ) --outFolder $build_dir --overwrite
    }
}

Function PackageScript
{
    exec {
        & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
    exec {
        & dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
    }
}


Function Package-Everything
{
    Write-Output "Packaging nuget packages"
    dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
    PackageUI
    PackageDatabase
    PackageAcceptanceTests
    PackageScript
}

Function PrivateBuild
{
    Log-Message "Starting Private Build" -Type "INFO"
    $projectConfig = "Debug"
    [Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
    $sw = [Diagnostics.Stopwatch]::StartNew()

    Init
    Compile
    UnitTests

    Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir
    if ($script:databaseInDocker)
    {
        Create-SqlServerInDocker -serverName $script:databaseServer -dbAction $databaseAction -scriptDir $script:databaseScripts
    }
    else
    {
        MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
    }

    IntegrationTest
    #AcceptanceTests

    Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir

    $sw.Stop()
    Log-Message "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -Type "INFO"
}

Function CIBuild
{
    Log-Message
    $sw = [Diagnostics.Stopwatch]::StartNew()

    Init
    Compile
    UnitTests

    MigrateDatabaseLocal  -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
    Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $databaseServer -sourceDir $source_dir

    IntegrationTest
    #AcceptanceTests
    Package-Everything
    $sw.Stop()
    Log-Message "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -Type "INFO"
}