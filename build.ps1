#!/usr/bin/env pwsh
. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
# Only clear for local builds; CI sets this intentionally (e.g. SQLite connection string)
if ($env:ConnectionStrings__SqlConnectionString -and -not (Test-IsGitHubActions) -and -not (Test-IsAzureDevOps)) {
	Log-Message "Clearing ConnectionStrings__SqlConnectionString environment variable" -Type "DEBUG"
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

$verbosity = "quiet"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Update" }

Function Set-DatabaseEngineForArm {
	$isArmArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -in @(
		[System.Runtime.InteropServices.Architecture]::Arm,
		[System.Runtime.InteropServices.Architecture]::Arm64
	)

	if ($isArmArchitecture) {
		$env:database_engine = "SQLite"
		$env:DATABASE_ENGINE = "SQLite"
		Log-Message -Message "ARM architecture detected. Forcing DATABASE_ENGINE=SQLite." -Type "DEBUG"
	}
}

Set-DatabaseEngineForArm
$script:databaseEngine = $env:DATABASE_ENGINE

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

Function Resolve-DatabaseEngine {
	if ([string]::IsNullOrEmpty($script:databaseEngine)) {
		if (Test-IsLinux) {
			if (Test-IsDockerRunning) {
				$script:databaseEngine = "SQL-Container"
			}
			else {
				$script:databaseEngine = "SQLite"
			}
		}
		else {
			$script:databaseEngine = "LocalDB"
		}
		Log-Message -Message "DATABASE_ENGINE not set. Auto-detected: $script:databaseEngine" -Type "DEBUG"
	}
	else {
		$validEngines = @("LocalDB", "SQL-Container", "SQLite")
		if ($script:databaseEngine -notin $validEngines) {
			throw "Invalid DATABASE_ENGINE value '$($script:databaseEngine)'. Valid values: $($validEngines -join ', ')"
		}
		Log-Message -Message "DATABASE_ENGINE set to: $script:databaseEngine" -Type "DEBUG"
	}
	$script:useSqlite = ($script:databaseEngine -eq "SQLite")
}

Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		Log-Message -Message "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell" -Type "WARNING"
		throw "PowerShell 7 is required to run this build script."
	}
 	else {
		Log-Message -Message "PowerShell 7 found at: $pwshPath" -Type "DEBUG"
	}

	if (Test-IsAzureDevOps) { 
		Log-Message -Message "Running in Azure DevOps Pipeline" -Type "DEBUG"
	}
	else {
		Log-Message -Message "Running in Local Environment" -Type "DEBUG"
	}

	if (Test-IsLinux) {
		Log-Message -Message "Running on Linux" -Type "DEBUG"
		# Set NuGet cache to shorter path for Linux/WSL compatibility (only for local builds)
		if (-not (Test-IsAzureDevOps) -and -not (Test-IsGitHubActions)) {
			$env:NUGET_PACKAGES = "/tmp/nuget-packages"
			Log-Message -Message "Setting NUGET_PACKAGES to /tmp/nuget-packages for WSL" -Type "DEBUG"
		}
	}
	elseif (Test-IsWindows) {
		Log-Message -Message "Running on Windows" -Type "DEBUG"
	}

	switch ($script:databaseEngine) {
		"LocalDB" {
			if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }
		}
		"SQL-Container" {
			if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
			if (Test-IsDockerRunning) {
				Log-Message -Message "Docker is running" -Type "DEBUG"
			} else {
				Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
				throw "Docker is not running."
			}
		}
		"SQLite" {
			if ([string]::IsNullOrEmpty($env:ConnectionStrings__SqlConnectionString)) {
				$env:ConnectionStrings__SqlConnectionString = "Data Source=ChurchBulletin.db"
				Log-Message -Message "Set ConnectionStrings__SqlConnectionString for SQLite: $($env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
			}
			Log-Message -Message "SQLite mode enabled - Docker not required" -Type "DEBUG"
		}
	}

	if ($script:databaseEngine -ne "SQLite") {
		Log-Message "Using $script:databaseServer as database server." -Type "DEBUG"
		Log-Message "Using $script:databaseName as the database name." -Type "DEBUG"
	}

	Log-Message -Message "Cleaning build artifacts and initializing environment..." -Type "INFO"

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}
	
	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $solutionName -nologo -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}

	Log-Message -Message "Restoring NuGet packages..." -Type "INFO"
	exec {
		& dotnet restore $solutionName -nologo --interactive -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}
	
	Log-Message -Message "Project Config: $projectConfig" -Type "DEBUG"
	Log-Message -Message "Version: $version" -Type "DEBUG"
}

Function Compile {
	exec {
		& dotnet build $solutionName -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:MSBuildTreatAllWarningsAsErrors="true" `
			/p:SuppressNETCoreSdkPreviewMessage=true `
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

	Log-Message -Message "Checking Playwright browsers for Acceptance Tests" -Type "DEBUG"
	$playwrightScript = Join-PathSegments "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		Log-Message -Message "Playwright script found at $playwrightScript." -Type "DEBUG"
		
		# Ensure Playwright chromium is installed (idempotent - skips if already present)
		Log-Message -Message "Ensuring Playwright chromium browser is installed..." -Type "DEBUG"
		& pwsh $playwrightScript install chromium --with-deps
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to install Playwright chromium"
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

	Log-Message -Message "Running Acceptance Tests" -Type "DEBUG"
	$runSettingsPath = Join-Path $acceptanceTestProjectPath "AcceptanceTests.runsettings"
	try {
		exec {
		& dotnet test /p:CopyLocalLockFileAssemblies=true -nologo -v normal --logger:trx `
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

	$env:ConnectionStrings__SqlConnectionString = "server=$serverName;database=$tempDatabaseName;User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
	Log-Message "Set ConnectionStrings__SqlConnectionString for process: $(Get-RedactedConnectionString -ConnectionString $env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	$dbArgs = @($databaseDll, $dbAction, $serverName, $tempDatabaseName, $scriptDir, "sa", $sqlPassword)
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
	# Restore connection string to default project database
	$env:ConnectionStrings__SqlConnectionString = "server=$($script:databaseServer);database=$projectName;User ID=sa;Password=$(Get-SqlServerPassword -ContainerName $(Get-ContainerName -DatabaseName $projectName));TrustServerCertificate=true;"
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

	# Copy the .playwright metadata folder into the publish output so the nupkg
	# is self-contained.  The playwright.ps1 install command needs this folder to
	# know which browser versions to download on the target machine.
	$publishDir = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework "publish"
	$playwrightSource = Join-PathSegments $acceptanceTestProjectPath "bin" "Debug" $framework ".playwright"
	if (Test-Path $playwrightSource) {
		Log-Message "Copying .playwright metadata into publish output" -Type "DEBUG"
		Copy-Item -Path $playwrightSource -Destination (Join-Path $publishDir ".playwright") -Recurse -Force
	} else {
		Log-Message "WARNING: .playwright folder not found at $playwrightSource" -Type "WARNING"
	}

	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $publishDir --outFolder $build_dir --overwrite
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

	# Allow Octopus.DotNet.Cli (targets net6.0) to run on the current .NET SDK
	$env:DOTNET_ROLL_FORWARD = "LatestMajor"

	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
	
	# Ensure dotnet tools are in PATH
	$dotnetToolsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
	$pathEntries = $env:PATH -split [System.IO.Path]::PathSeparator
	$dotnetToolsPathPresent = $pathEntries | Where-Object { $_.Trim().ToLowerInvariant() -eq $dotnetToolsPath.Trim().ToLowerInvariant() }
	if (-not $dotnetToolsPathPresent) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
		Log-Message -Message "Added dotnet tools to PATH: $dotnetToolsPath" -Type "DEBUG"
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
When Docker is not available on Linux, automatically falls back to SQLite.

Build steps:
1. Init - Clean and restore NuGet packages
2. Compile - Build the solution
3. Database setup - Create SQL Server (Docker on Linux, LocalDB on Windows), or skip if SQLite
4. Update appsettings.json files with connection strings (skipped for SQLite)
5. MigrateDatabaseLocal - Run database migrations (skipped for SQLite; uses EnsureCreated)
6. AcceptanceTests - Run Playwright browser-based acceptance tests (auto-installs browsers)
7. Restore appsettings.json files to git state (skipped for SQLite)

.PARAMETER databaseServer
Optional. Specifies the database server to use. If not provided, defaults to "localhost"
on Linux or "(LocalDb)\MSSQLLocalDB" on Windows. Ignored when using SQLite.

.PARAMETER databaseName
Optional. Specifies the database name to use. If not provided, generates a unique name
based on the project name and timestamp (Windows only). Ignored when using SQLite.

.PARAMETER UseSqlite
Optional switch. Forces SQLite mode, bypassing SQL Server Docker setup and database migrations.
Auto-detected on Linux when Docker is not running.

.EXAMPLE
Invoke-AcceptanceTests

.EXAMPLE
Invoke-AcceptanceTests -databaseServer "localhost" -databaseName "ChurchBulletin_Test"

.EXAMPLE
Invoke-AcceptanceTests -UseSqlite

.NOTES
Requires Playwright browsers installed. Azure OpenID recommended for AI tests.
Automatically installs Playwright browsers if not present.
Falls back to SQLite when Docker is unavailable on Linux.
#>
Function Invoke-AcceptanceTests {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",
		[Parameter(Mandatory=$false)]
		[string]$databaseName ="",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)


	Log-Message -Message "Starting AcceptanceBuild..." -Type "INFO"
	$projectConfig = "Release"
	$sw = [Diagnostics.Stopwatch]::StartNew()

	# Override database engine if -UseSqlite switch is provided
	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	# Resolve database engine from DATABASE_ENGINE env var or auto-detection
	Resolve-DatabaseEngine

	# Set database server from parameter if provided
	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
		}
	}

	# Generate unique database name for this build instance
	if ($script:databaseEngine -ne "SQLite") {
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
	}

	Init

	Log-Message -Message "Compiling solution..." -Type "INFO"
	Compile

	Log-Message -Message "Setting up database dependencies..." -Type "INFO"
	if ($script:databaseEngine -eq "SQL-Container") {
		Log-Message -Message "Setting up SQL Server in Docker" -Type "DEBUG"
		New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
		New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		$containerName = Get-ContainerName -DatabaseName $script:databaseName
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$env:ConnectionStrings__SqlConnectionString = "server=$($script:databaseServer);database=$($script:databaseName);User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
		Log-Message "Set ConnectionStrings__SqlConnectionString for process: $(Get-RedactedConnectionString -ConnectionString $env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	elseif ($script:databaseEngine -eq "LocalDB") {
		$env:ConnectionStrings__SqlConnectionString = "server=$($script:databaseServer);database=$($script:databaseName);Integrated Security=true;"
		Log-Message "Set ConnectionStrings__SqlConnectionString for process: $($env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	else {
		Log-Message -Message "Skipping SQL Server setup and database migration (using SQLite with EnsureCreated)" -Type "DEBUG"
	}

	Log-Message -Message "Running acceptance tests..." -Type "INFO"
	AcceptanceTests

	$sw.Stop()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED (SQLite) - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	}
	else {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
		Log-Message -Message "Database used: $script:databaseName" -Type "DEBUG"
	}
}

<#
.SYNOPSIS
Core build pipeline: init, compile, test, database setup, and integration tests.

.DESCRIPTION
Invoke-CoreBuild contains the shared build steps used by the Build function.
It is not intended to be called directly.

Build steps:
1. Init - Clean build artifacts and restore NuGet packages
2. Compile - Build the solution
3. UnitTests - Run unit tests
4. Database setup - Create/migrate database (SQL-Container, LocalDB, or SQLite)
5. IntegrationTest - Run integration tests
#>
Function Invoke-CoreBuild {
	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	Init

	Log-Message -Message "Compiling solution..." -Type "INFO"
	Compile

	Log-Message -Message "Running unit tests..." -Type "INFO"
	UnitTests

	Log-Message -Message "Setting up database dependencies..." -Type "INFO"
	if ($script:databaseEngine -eq "SQL-Container") {
		Log-Message -Message "Setting up SQL Server in Docker" -Type "DEBUG"
		New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
		New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		$containerName = Get-ContainerName -DatabaseName $script:databaseName
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$env:ConnectionStrings__SqlConnectionString = "server=$($script:databaseServer);database=$($script:databaseName);User ID=sa;Password=$sqlPassword;TrustServerCertificate=true;"
		Log-Message "Set ConnectionStrings__SqlConnectionString for process: $(Get-RedactedConnectionString -ConnectionString $env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	elseif ($script:databaseEngine -eq "LocalDB") {
		$env:ConnectionStrings__SqlConnectionString = "server=$($script:databaseServer);database=$($script:databaseName);Integrated Security=true;"
		Log-Message "Set ConnectionStrings__SqlConnectionString for process: $($env:ConnectionStrings__SqlConnectionString)" -Type "DEBUG"
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	else {
		Log-Message -Message "Skipping SQL Server setup and database migration (using SQLite with EnsureCreated)" -Type "DEBUG"
	}

	Log-Message -Message "Running integration tests..." -Type "INFO"
	IntegrationTest

	$script:buildStopwatch.Stop()
}

<#
.SYNOPSIS
Executes a full build with unit and integration tests.

.DESCRIPTION
Wrapper around Invoke-CoreBuild used by both local developer builds and CI/CD pipelines.
Accepts optional parameters for database server, database name, and SQLite mode.
Auto-detects the build environment (Azure DevOps, GitHub Actions, or local).
Does NOT create NuGet packages; packaging is handled separately by Package-Everything.

.PARAMETER databaseServer
Optional. Specifies the database server. Defaults to "localhost" on Linux or "(LocalDb)\MSSQLLocalDB" on Windows.

.PARAMETER databaseName
Optional. Specifies the database name. If not provided, auto-generated based on environment.

.PARAMETER UseSqlite
Optional switch. Forces SQLite mode, bypassing SQL Server setup.

.EXAMPLE
Build

.EXAMPLE
Build -databaseServer "localhost"

.EXAMPLE
Build -UseSqlite
#>
Function Build {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",

		[Parameter(Mandatory = $false)]
		[string]$databaseName = "",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)

	if (Test-IsAzureDevOps) {
		Log-Message -Message "Starting Build on Azure DevOps..." -Type "INFO"
	}
	elseif (Test-IsGitHubActions) {
		Log-Message -Message "Starting Build on GitHub Actions..." -Type "INFO"
	}
	else {
		Log-Message -Message "Starting Build..." -Type "INFO"
	}

	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
			Log-Message -Message "Using database server from parameter: $script:databaseServer" -Type "DEBUG"
		}

		if (-not [string]::IsNullOrEmpty($databaseName)) {
			$script:databaseName = $databaseName
			Log-Message -Message "Using database name from parameter: $script:databaseName" -Type "DEBUG"
		}
		elseif (Test-IsLinux -or Test-IsLocalBuild) {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
		}
		else {
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
		}
	}

	Invoke-CoreBuild

	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "BUILD SUCCEEDED (SQLite) - Build time: $($script:buildStopwatch.Elapsed.ToString())" -Type "INFO"
	}
	else {
		Log-Message -Message "BUILD SUCCEEDED - Build time: $($script:buildStopwatch.Elapsed.ToString())" -Type "INFO"
		Log-Message -Message "Database used: $script:databaseName" -Type "DEBUG"
	}
}
