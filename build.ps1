#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString) {
	Log-Message "ConnectionStrings__SqlConnectionString is set" -Type "INFO"
}

$script:projectName = "ChurchBulletin"
$script:projectConfig = $env:BuildConfiguration
if ([string]::IsNullOrEmpty($script:projectConfig)) { $script:projectConfig = "Release" }


$base_dir = resolve-path .\
$source_dir = Join-Path $base_dir "src"
$solutionName = Join-Path $source_dir "$script:projectName.sln"
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath =  Join-PathSegments $source_dir "UI" "Server" 
$databaseProjectPath = Join-Path $source_dir "Database"

$framework = "net10.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Update" }

$databaseName = $script:projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $script:projectName }

#$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }

 


Function Compile {
	exec {
		& dotnet build $solutionName -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $script:projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="Church Bulletin"
	}
}

Function UnitTests {
	Push-Location -Path $unitTestProjectPath

	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "UnitTests") --no-build `
				--no-restore --configuration $script:projectConfig `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function IntegrationTest {
	Push-Location -Path $integrationTestProjectPath

	try {
		#  --settings path/to/your.runsettings
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
				--no-restore --configuration $script:projectConfig `
				--collect:"XPlat Code Coverage"
		}
	}
	finally {
		Pop-Location
	}
}

Function AcceptanceTests {
	# Check if UI.Server is already running
	$uiServerProcess = Get-Process -Name "ClearMeasure.Bootcamp.UI.Server" -ErrorAction SilentlyContinue
	if ($uiServerProcess) {
		Log-Message -Message "Warning: ClearMeasure.Bootcamp.UI.Server is already running in background (PID: $($uiServerProcess.Id)). This may interfere with acceptance tests." -Type "WARNING"
	}

	$runSettingsPath = Join-Path $acceptanceTestProjectPath "AcceptanceTests.runsettings"
    Push-Location -Path $acceptanceTestProjectPath
    try {
        $playwrightScript = Join-PathSegments (Get-Location) "bin" "Release" "net10.0" "playwright.ps1"
        Install-Playwright $playwrightScript
        
        exec {
            & dotnet test /p:CopyLocalLockFileAssemblies=true /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
                --results-directory $(Join-Path $test_dir "AcceptanceTests") --no-build `
                --no-restore --configuration $script:projectConfig `
                --settings:$runSettingsPath `
                --collect:"XPlat Code Coverage"
        }
    }
    finally {
        Pop-Location
    }

}

Function MigrateDatabaseLocal {
	param (
	 [Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseServerFunc,
		
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseNameFunc,
		
		[Parameter(Mandatory = $false)]
		[string]$databaseUser = "",
		
		[Parameter(Mandatory = $false)]
		[string]$databasePassword = ""
	)
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $script:projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	
	if (Test-IsLinux) {
		if ([string]::IsNullOrWhiteSpace($databaseUser)) {
			$containerName = Get-ContainerName -DatabaseName $databaseNameFunc
			$databaseUser = "sa"
			$databasePassword = Get-SqlServerPassword -ContainerName $containerName
		}
		$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts, $databaseUser, $databasePassword)
	}
	else {
		if (-not [string]::IsNullOrWhiteSpace($databaseUser)) {
			$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts, $databaseUser, $databasePassword)
		}
		else {
			$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts)
		}
	}
	
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
}

Function Publish-ToGitHubPackages {
	param(
		[Parameter(Mandatory = $true)]
		[string]$packageId
	)
	
	$githubToken = $env:GITHUB_TOKEN
	if ([string]::IsNullOrEmpty($githubToken)) {
		Log-Message -Message "GITHUB_TOKEN not found. Cannot publish $packageId to GitHub Packages." -Type "ERROR"
		throw "GITHUB_TOKEN environment variable is required for publishing to GitHub Packages"
	}
	
	# Verify token is not empty (but don't log the actual token)
	if ($githubToken.Length -lt 10) {
		Log-Message -Message "GITHUB_TOKEN appears to be invalid (too short)." -Type "ERROR"
		throw "GITHUB_TOKEN appears to be invalid"
	}
	
	Log-Message -Message "GITHUB_TOKEN found (length: $($githubToken.Length))" -Type "INFO"
	
	$githubRepo = $env:GITHUB_REPOSITORY
	if ([string]::IsNullOrEmpty($githubRepo)) {
		Log-Message -Message "GITHUB_REPOSITORY not found. Cannot determine GitHub Packages feed." -Type "ERROR"
		throw "GITHUB_REPOSITORY environment variable is required"
	}
	
	$owner = $githubRepo.Split('/')[0]
	$githubFeed = "https://nuget.pkg.github.com/$owner/index.json"
	
	$packageFile = Get-ChildItem "$build_dir/$packageId.$version.nupkg" -ErrorAction SilentlyContinue
	if (-not $packageFile) {
		Log-Message -Message "Package file not found: $packageId.$version.nupkg" -Type "ERROR"
		throw "Package file not found: $packageId.$version.nupkg"
	}
	
	Log-Message -Message "Publishing $($packageFile.Name) to GitHub Packages..." -Type "INFO"
	Log-Message -Message "Feed: $githubFeed" -Type "INFO"
	Log-Message -Message "Owner: $owner" -Type "INFO"
	
	# GitHub Packages authentication: use username (owner) and token as password
	# Configure source first, then push
	$sourceName = "GitHub-$owner"
	
	# Remove existing source if it exists
	$existingSource = dotnet nuget list source | Select-String -Pattern $sourceName -Quiet
	if ($existingSource) {
		Log-Message -Message "Removing existing source: $sourceName" -Type "INFO"
		dotnet nuget remove source $sourceName 2>$null
	}
	
	# Add source with username/password authentication
	Log-Message -Message "Adding NuGet source with authentication..." -Type "INFO"
	exec {
		& dotnet nuget add source $githubFeed --name $sourceName --username $owner --password $githubToken --store-password-in-clear-text
	}
	
	# Push using the source with explicit API key (credentials stored with source, but API key ensures authentication)
	Log-Message -Message "Pushing package to GitHub Packages..." -Type "INFO"
	Log-Message -Message "Feed: $githubFeed" -Type "INFO"
	Log-Message -Message "Owner: $owner" -Type "INFO"
	exec {
		# Try with source name first (uses stored credentials)
		& dotnet nuget push $packageFile.FullName --source $sourceName --api-key $githubToken --skip-duplicate
	}
	
	Log-Message -Message "Successfully published $($packageFile.Name) to GitHub Packages" -Type "INFO"
}

Function PackageUI {    
	$packageName = "$script:projectName.UI.$version.nupkg"
	$packagePath = Join-Path $build_dir $packageName

	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $script:projectConfig
	}
	
	exec {
		& dotnet-octo pack --id "$script:projectName.UI" --version $version --basePath $(Join-PathSegments $uiProjectPath "bin" $script:projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}
	
}

Function PackageDatabase {    
	exec {
		& dotnet publish $databaseProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$script:projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}
	

}

Function PackageAcceptanceTests {  
	# Use Debug configuration so full symbols are available to display better error messages in test failures
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$script:projectName.AcceptanceTests" --version $version --basePath $(Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework "publish") --outFolder $build_dir --overwrite
	}
}

Function PackageScript {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $script:projectConfig
	}
	exec {
		& dotnet-octo pack --id "$script:projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}	
}


Function Package-Everything{
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

<#
.SYNOPSIS
Executes a complete build with acceptance tests and packaging.

.DESCRIPTION
The Invoke-AcceptanceTests function performs a full build including initialization, compilation, 
database setup, acceptance tests with Playwright browser automation, and NuGet package creation. 
This function is designed for end-to-end validation before deployment.

On Linux, creates a SQL Server Docker container with a non-unique database name.
On Windows, uses LocalDB with a unique timestamp-based database name.

Build steps:
1. Init - Clean and restore NuGet packages
2. Compile - Build the solution
3. Database setup - Create SQL Server (Docker on Linux, LocalDB on Windows)
4. Update appsettings.json files with connection strings
5. Temporarily disable ConnectionStrings in launchSettings.json to prevent override
6. MigrateDatabaseLocal - Run database migrations
7. AcceptanceTests - Run Playwright browser-based acceptance tests (auto-installs browsers)
8. Restore appsettings.json and launchSettings.json files to git state
9. Package-Everything - Create NuGet packages for deployment

.PARAMETER databaseServer
Optional. Specifies the database server to use. If not provided, defaults to "localhost" 
on Linux or "(LocalDb)\MSSQLLocalDB" on Windows.

.PARAMETER databaseName
Optional. Specifies the database name to use. If not provided, generates a unique name 
based on the project name and timestamp (Windows only).

.EXAMPLE
Invoke-AcceptanceTests

.EXAMPLE
Invoke-AcceptanceTests -databaseServer "localhost" -databaseName "ChurchBulletin_Test"

.NOTES
Requires Docker on Linux, Ollama running for AI tests, and Playwright browsers installed.
Sets containerAppURL environment variable to "localhost:7174".
Automatically installs Playwright browsers if not present.
#>
Function Invoke-AcceptanceTests {
	param (
		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
		[string]$databaseServer,
		[Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
		[string]$databaseName
	)
	
	Log-Message -Message "Starting AcceptanceBuild..." -Type "INFO"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()

    $isOllamaRunning = Test-IsOllamaRunning -LogOutput $true
	
	Init
	Compile

	if (Test-IsLinux) 
	{
		if (Test-IsDockerRunning) 
		{
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $databaseName)
			New-SqlServerDatabase -databaseServer $databaseServer -databaseName $databaseName
		}
		else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}

	Update-AppSettingsConnectionStrings -databaseName $databaseName -databaseServer $databaseServer -sourceDir $source_dir
	
	# Temporarily disable ConnectionStrings in launchSettings.json for acceptance tests
	# This prevents the Windows LocalDB connection string from overriding appsettings.json
	$launchSettingsPath = Join-PathSegments $source_dir "UI" "Server" "Properties" "launchSettings.json"
	if (Test-Path $launchSettingsPath) {
		$launchSettings = Get-Content $launchSettingsPath -Raw
		$launchSettings = $launchSettings -replace '"ConnectionStrings__SqlConnectionString":', '"_DISABLED_ConnectionStrings__SqlConnectionString":'
		Set-Content -Path $launchSettingsPath -Value $launchSettings
	}
	
	MigrateDatabaseLocal -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
	AcceptanceTests
	
	# Restore appsettings and launchSettings files to their original git state
	Log-Message -Message "Restoring appsettings*.json and launchSettings.json files to git state" -Type "INFO"
	& git restore 'src/**/appsettings*.json'
	if ($LASTEXITCODE -ne 0) {
		Log-Message -Message "Warning: Failed to restore appsettings*.json files" -Type "WARNING"
	}
	if (Test-Path $launchSettingsPath) {
		& git restore $launchSettingsPath
		if ($LASTEXITCODE -ne 0) {
			Log-Message -Message "Warning: Failed to restore launchSettings.json file" -Type "WARNING"
		}
	}

	$sw.Stop()
	Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
}

<#
.SYNOPSIS
Executes a local developer build with unit and integration tests.

.DESCRIPTION
The Invoke-PrivateBuild function runs a complete build process for local development including 
initialization, compilation, unit tests, database setup, and integration tests. This function 
is intended for developers to validate changes locally before committing code.

On Linux, creates a SQL Server Docker container with a non-unique database name.
On Windows, uses LocalDB with a unique timestamp-based database name to avoid conflicts.

Build steps:
1. Init - Clean and restore NuGet packages
2. Compile - Build the solution
3. UnitTests - Run unit tests
4. Database setup - Create SQL Server (Docker on Linux, LocalDB on Windows)
5. Update appsettings.json files with connection strings
6. MigrateDatabaseLocal - Run database migrations
7. IntegrationTest - Run integration tests
8. Restore appsettings.json files to git state

Note: Does NOT create NuGet packages. Use Invoke-CIBuild or Invoke-AcceptanceTests for packaging.

.PARAMETER databaseServer
Optional. Specifies the database server to use. If not provided, defaults to "localhost" 
on Linux or "(LocalDb)\MSSQLLocalDB" on Windows.

.EXAMPLE
Invoke-PrivateBuild

.EXAMPLE
Invoke-PrivateBuild -databaseServer "localhost"

.NOTES
Requires Docker on Linux. Sets containerAppURL environment variable to "localhost:7174".
#>
Function Invoke-PrivateBuild {
	param (
		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
		[string]$databaseServer,
        
		[Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
		[string]$databaseName 
	)
	
	Log-Message -Message "Starting Invoke-PrivateBuild, setting the environment variable ContainerAppURL to localhost:7174" -Type "INFO"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")

	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	Init
	Compile
	UnitTests
	
	if (Test-IsLinux) 
	{
		if (Test-IsDockerRunning ) 
		{
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $databaseName)
			New-SqlServerDatabase -databaseServer $databaseServer -databaseName $databaseName
		}
		else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}
	
	Update-AppSettingsConnectionStrings -databaseServer $databaseServer -databaseName $databaseName  -sourceDir $source_dir
	Drop-SqlServerDatabase -databaseServer $databaseServer -databaseName $databaseName
	MigrateDatabaseLocal -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
	
	IntegrationTest

	# Restore appsettings files to their original git state
	Log-Message -Message "Restoring appsettings*.json files to git state" -Type "INFO"
	& git restore 'src/**/appsettings*.json'
	if ($LASTEXITCODE -ne 0) {
		Log-Message -Message "Warning: Failed to restore appsettings*.json files" -Type "WARNING"
	}
	
	$sw.Stop()
	Log-Message -Message "PRIVATE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
}

<#
.SYNOPSIS
Executes a complete continuous integration build pipeline.

.DESCRIPTION
The Invoke-CIBuild function runs a full CI/CD build process including initialization, compilation, 
unit tests, database setup, integration tests, and packaging. This function is designed to 
run in CI/CD environments (Azure DevOps, GitHub Actions) or locally.

On Linux, creates a SQL Server Docker container with a non-unique database name.
On Windows, uses LocalDB with a unique database name.

Build steps:
1. Init - Clean and restore NuGet packages
2. Compile - Build the solution
3. UnitTests - Run unit tests
4. Database setup - Create SQL Server (Docker on Linux, LocalDB on Windows)
5. Update appsettings.json files with connection strings
6. MigrateDatabaseLocal - Run database migrations
7. IntegrationTest - Run integration tests
8. Restore appsettings.json files to git state
9. Package-Everything - Create NuGet packages for deployment

.EXAMPLE
Invoke-CIBuild

.NOTES
Requires Docker on Linux. Uses Test-IsLinux, Test-IsAzureDevOps, and Test-IsGitHubActions 
to detect environment and adjust behavior accordingly.
#>
Function Invoke-CIBuild {
    
	if (Test-IsAzureDevOps) {
		Log-Message -Message "Starting Invoke-CIBuild on Azure DevOps..." -Type "INFO"
	}
	elseif (Test-IsGitHubActions) {
		Log-Message -Message "Starting Invoke-CIBuild on GitHub Actions..." -Type "INFO"
	}
	else {
		Log-Message -Message "Starting Invoke-CIBuild..." -Type "INFO"
	}
    $connectionString = Get-ConnectionStringComponents
    if ($connectionString.IsEmpty) {
        throw "ConnectionStrings__SqlConnectionString is required for Invoke-CIBuild."
    }
    
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate database name based on environment
	# On Linux with Docker, no need for unique names since container is clean
	# On local Windows builds, use simple name. On CI, use unique name to avoid conflicts.
#	if (Test-IsLinux) {
#		$script:databaseName = Generate-UniqueDatabaseName -baseName $connectionString.Database -generateUnique $false
#	}
#	else {
#		$script:databaseName = Generate-UniqueDatabaseName -baseName $connectionString.Database -generateUnique $true
#	}
	
	Init
	Compile
	UnitTests

	if (Test-IsLinux) 
	{
		if (Test-IsDockerRunning ) 
		{
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $connectionString.Database)
			New-SqlServerDatabase -serverName $connectionString.Server -databaseName $connectionString.Database
		}
		else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}

	Update-AppSettingsConnectionStrings -databaseServer $connectionString.Server -databaseName $connectionString.Database -sourceDir $source_dir
	MigrateDatabaseLocal -databaseServerFunc $connectionString.Server -databaseNameFunc $connectionString.Database -databaseUser $connectionString.User -databasePassword $connectionString.Password
	
	IntegrationTest
	# Restore appsettings files to their original git state
	Log-Message -Message "Restoring appsettings*.json files to git state" -Type "INFO"
	& git restore 'src/**/appsettings*.json'
	if ($LASTEXITCODE -ne 0) {
		Log-Message -Message "Warning: Failed to restore appsettings*.json files" -Type "WARNING"
	}
	
	# Package-Everything

	$sw.Stop()
	Log-Message -Message "Invoke-CIBuild SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
}
