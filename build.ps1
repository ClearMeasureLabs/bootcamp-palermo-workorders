#!/usr/bin/env pwsh
. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString) {
	Log-Message "Clearing ConnectionStrings__SqlConnectionString environment variable" -Type "INFO"
	$env:ConnectionStrings__SqlConnectionString = $null
	[Environment]::SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", $null, "User")
}

$projectName = "ChurchBulletin"
$base_dir = resolve-path .\
$source_dir = Join-Path $base_dir "src"
$solutionName = Join-Path $source_dir "$projectName.sln"
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath =  Join-PathSegments $source_dir "UI" "Server" 
$databaseProjectPath = Join-Path $source_dir "Database"
$projectConfig = $env:BuildConfiguration
$framework = "net10.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Update" }

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

 
Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		Log-Message -Message "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell" -Type "WARNING"
		throw "PowerShell 7 is required to run this build script."
	}
 	else {
		Log-Message -Message "PowerShell 7 found at: $pwshPath" -Type "INFO"
	}

	if (Test-IsAzureDevOps) { 
		Log-Message -Message "Running in Azure DevOps Pipeline" -Type "INFO"
	}
	else {
		Log-Message -Message "Running in Local Environment" -Type "INFO"
	}

	if (Test-IsLinux) {
		# Set NuGet cache to shorter path for Linux/WSL compatibility (only for local builds)
		if (-not (Test-IsAzureDevOps) -and -not (Test-IsGitHubActions)) {
			$env:NUGET_PACKAGES = "/tmp/nuget-packages"
			Log-Message -Message "Setting NUGET_PACKAGES to /tmp/nuget-packages for WSL" -Type "INFO"
		}

		Log-Message -Message "Running on Linux" -Type "INFO"
		if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
		if ($script:useSqlite) {
			Log-Message -Message "SQLite mode enabled - Docker not required" -Type "INFO"
		}
		elseif (Test-IsDockerRunning) {
			Log-Message -Message "Docker is running" -Type "INFO"
		} else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}
	elseif (Test-IsWindows) {
		if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }
		Log-Message -Message "Running on Windows" -Type "INFO"
	}
	Log-Message "Using $script:databaseServer as database server." -Type "INFO"
	Log-Message "Using $script:databaseName as the database name." 

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}
	
	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $solutionName -nologo -v $verbosity
	}
	exec {
		& dotnet restore $solutionName -nologo --interactive -v $verbosity  
	}
	
	Log-Message -Message "Project Config: $projectConfig" -Type "INFO"
	Log-Message -Message "Version: $version" -Type "INFO"
}

Function Compile {
	exec {
		& dotnet build $solutionName -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="Church Bulletin"
	}
}

Function UnitTests {
	Push-Location -Path $unitTestProjectPath

	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "UnitTests") --no-build `
				--no-restore --configuration $projectConfig `
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
		$testFilter = ""
		if ($script:useSqlite) {
			$testFilter = '--filter "TestCategory!=SqlServerOnly"'
		}
		exec {
			if ($script:useSqlite) {
				& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
					--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
					--no-restore --configuration $projectConfig `
					--collect:"XPlat Code Coverage" `
					--filter "TestCategory!=SqlServerOnly"
			}
			else {
				& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
					--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
					--no-restore --configuration $projectConfig `
					--collect:"XPlat Code Coverage"
			}
		}
	}
	finally {
		Pop-Location
	}
}

Function AcceptanceTests {
	$projectConfig = "Release"
	Push-Location -Path $acceptanceTestProjectPath

	Log-Message -Message "Checking Playwright browsers for Acceptance Tests" -Type "INFO"
	$playwrightScript = Join-PathSegments "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		Log-Message -Message "Playwright script found at $playwrightScript." -Type "INFO"
		
		# Check if browsers are installed
		try {
			$listOutput = & pwsh $playwrightScript list 2>&1 | Out-String
			if ($listOutput -match "chromium|webkit|firefox") {
				Log-Message -Message "Playwright browsers are installed." -Type "INFO"
			}
			else {
				Log-Message -Message "Playwright browsers not detected. Installing..." -Type "WARNING"
				& pwsh $playwrightScript install --with-deps
				if ($LASTEXITCODE -ne 0) {
					throw "Failed to install Playwright browsers"
				}
				Log-Message -Message "Playwright browsers installed successfully." -Type "INFO"
			}
		}
		catch {
			Log-Message -Message "WARNING: Could not verify Playwright browser installation. Attempting to install..." -Type "WARNING"
			& pwsh $playwrightScript install --with-deps
			if ($LASTEXITCODE -ne 0) {
				throw "Failed to install Playwright browsers"
			}
		}
	}
	else {
		throw "Playwright script not found at $playwrightScript. Cannot run acceptance tests without the browsers."
	}

	# Check if UI.Server is already running
	$uiServerProcess = Get-Process -Name "ClearMeasure.Bootcamp.UI.Server" -ErrorAction SilentlyContinue
	if ($uiServerProcess) {
		Log-Message -Message "Warning: ClearMeasure.Bootcamp.UI.Server is already running in background (PID: $($uiServerProcess.Id)). This may interfere with acceptance tests." -Type "WARNING"
	}

	Log-Message -Message "Running Acceptance Tests" -Type "INFO"
	$runSettingsPath = Join-Path $acceptanceTestProjectPath "AcceptanceTests.runsettings"
	try {
		exec {
			& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "AcceptanceTests") --no-build `
				--no-restore --configuration $projectConfig `
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
		[string]$databaseNameFunc
	)
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	
	if (Test-IsLinux) {
		$containerName = Get-ContainerName -DatabaseName $databaseNameFunc
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts, "sa", $sqlPassword)
	}
	else {
		$dbArgs = @($databaseDll, $databaseAction, $databaseServerFunc, $databaseNameFunc, $script:databaseScripts)
	}
	
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
}

Function Create-SqlServerInDocker {
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
	$tempDatabaseName = Generate-UniqueDatabaseName -baseName $script:projectName -generateUnique $true
	$containerName = Get-ContainerName -DatabaseName $tempDatabaseName
	$sqlPassword = Get-SqlServerPassword -ContainerName $containerName

	New-DockerContainerForSqlServer -containerName $containerName
	New-SqlServerDatabase -serverName $serverName -databaseName $tempDatabaseName 

	Update-AppSettingsConnectionStrings -databaseNameToUse $tempDatabaseName -serverName $serverName -sourceDir $source_dir
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	$dbArgs = @($databaseDll, $dbAction, $serverName, $tempDatabaseName, $scriptDir, "sa", $sqlPassword)
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
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
	$packageName = "$projectName.UI.$version.nupkg"
	$packagePath = Join-Path $build_dir $packageName

	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	
	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $(Join-PathSegments $uiProjectPath "bin" $projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}
	
}

Function PackageDatabase {    
	exec {
		& dotnet publish $databaseProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}
	

}

Function PackageAcceptanceTests {  
	# Use Debug configuration so full symbols are available to display better error messages in test failures
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $(Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework "publish") --outFolder $build_dir --overwrite
	}
	
	
}

Function PackageScript {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}	

}


Function Package-Everything{
	
	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
	
	# Ensure dotnet tools are in PATH
	$dotnetToolsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
	$pathEntries = $env:PATH -split [System.IO.Path]::PathSeparator
	$dotnetToolsPathPresent = $pathEntries | Where-Object { $_.Trim().ToLowerInvariant() -eq $dotnetToolsPath.Trim().ToLowerInvariant() }
	if (-not $dotnetToolsPathPresent) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
		Log-Message -Message "Added dotnet tools to PATH: $dotnetToolsPath" -Type "INFO"
	}
	
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
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",
		[Parameter(Mandatory=$false)]
		[string]$databaseName =""
	)
	

	Log-Message -Message "Starting AcceptanceBuild..." -Type "INFO"
	$projectConfig = "Release"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()

    Test-IsOllamaRunning -LogOutput $true


	# Set database server from parameter if provided
	if (-not [string]::IsNullOrEmpty($databaseServer)) {
		$script:databaseServer = $databaseServer
	}
	else {
		if (Test-IsLinux) {
			$script:databaseServer = "localhost"
		}
		else {
			$script:databaseServer = "(LocalDb)\MSSQLLocalDB"
		}
	}
	
	# Generate unique database name for this build instance
	# On Linux with Docker, no need for unique names since container is clean
	# On local Windows builds, use simple name. On CI, use unique name to avoid conflicts.
	if (-not [string]::IsNullOrEmpty($databaseName)) {
		$script:databaseName = $databaseName
	}
	else {
		if (Test-IsLinux) {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
		}
		elseif (Test-IsLocalBuild) {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
		}
		else {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
		}
	}
	
	Init
	Compile

	if (Test-IsLinux) 
	{
		Log-Message -Message "Setting up SQL Server in Docker" -Type "INFO"
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
			New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		}
		else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}

	# Update appsettings.json files before database migration
	Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir
	
	# Temporarily disable ConnectionStrings in launchSettings.json for acceptance tests
	# This prevents the Windows LocalDB connection string from overriding appsettings.json
	$launchSettingsPath = Join-PathSegments $source_dir "UI" "Server" "Properties" "launchSettings.json"
	if (Test-Path $launchSettingsPath) {
		Log-Message -Message "Temporarily disabling ConnectionStrings in launchSettings.json" -Type "INFO"
		$launchSettings = Get-Content $launchSettingsPath -Raw
		$launchSettings = $launchSettings -replace '"ConnectionStrings__SqlConnectionString":', '"_DISABLED_ConnectionStrings__SqlConnectionString":'
		Set-Content -Path $launchSettingsPath -Value $launchSettings
	}
	
	MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
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
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
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
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",

		[Parameter(Mandatory = $false)]
		[string]$databaseName = "",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)

	Log-Message -Message "Starting Invoke-PrivateBuild..." -Type "INFO"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")

	# Auto-detect SQLite mode: use SQLite on Linux when Docker cannot run SQL Server
	if (-not $UseSqlite -and (Test-IsLinux)) {
		if (-not (Test-IsDockerRunning)) {
			Log-Message -Message "Docker is not available. Falling back to SQLite for integration tests." -Type "INFO"
			$UseSqlite = $true
		}
	}

	# Set script-level flag so Init can skip Docker check
	$script:useSqlite = $UseSqlite

	if ($UseSqlite) {
		Log-Message -Message "Using SQLite for integration tests (no SQL Server required)" -Type "INFO"
	}

	# Set database server from parameter if provided
	if (-not $UseSqlite) {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
			Log-Message -Message "Using database server from parameter: $script:databaseServer" -Type "INFO"
		}
		else {
			if (Test-IsLinux) {
				$script:databaseServer = "localhost"
			}
			else {
				$script:databaseServer = "(LocalDb)\MSSQLLocalDB"
			}
			Log-Message -Message "Using default database server for platform: $script:databaseServer" -Type "INFO"
		}
	}

	# Generate unique database name for this build instance
	if (-not $UseSqlite) {
		if (-not [string]::IsNullOrEmpty($databaseName)) {
			$script:databaseName = $databaseName
			Log-Message -Message "Using database name from parameter: $script:databaseName" -Type "INFO"
		}
		elseif (Test-IsLinux) {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
		}
		elseif (Test-IsLocalBuild) {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
		}
		else {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
		}
	}

	$sw = [Diagnostics.Stopwatch]::StartNew()

	Init
	Compile
	UnitTests

	if (-not $UseSqlite) {
		if (Test-IsLinux)
		{
			Log-Message -Message "Setting up SQL Server in Docker" -Type "INFO"
			if (Test-IsDockerRunning -LogOutput $true)
			{
				New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
				New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
			}
			else {
				Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
				throw "Docker is not running."
			}
		}

		# Update appsettings.json files before database migration
		Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir

		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	else {
		Log-Message -Message "Skipping SQL Server setup and AliaSQL migration (using SQLite with EnsureCreated)" -Type "INFO"
	}

	IntegrationTest

	if (-not $UseSqlite) {
		# Restore appsettings files to their original git state
		Log-Message -Message "Restoring appsettings*.json files to git state" -Type "INFO"
		& git restore 'src/**/appsettings*.json'
		if ($LASTEXITCODE -ne 0) {
			Log-Message -Message "Warning: Failed to restore appsettings*.json files" -Type "WARNING"
		}
	}

	$sw.Stop()
	if ($UseSqlite) {
		Log-Message -Message "PRIVATE BUILD SUCCEEDED (SQLite) - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	}
	else {
		Log-Message -Message "PRIVATE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
		Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
	}
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
	
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate database name based on environment
	# On Linux with Docker, no need for unique names since container is clean
	# On local Windows builds, use simple name. On CI, use unique name to avoid conflicts.
	if (Test-IsLinux) {
		$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
	}
	else {
		$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
	}
	
	Init
	Compile
	UnitTests

	if (Test-IsLinux) 
	{
		Log-Message -Message "Setting up SQL Server in Docker for CI" -Type "INFO"
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
			New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		}
		else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}

	Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir
	MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	
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
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}
