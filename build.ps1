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
$uiProjectPath = Join-Path $source_dir "UI" -AdditionalChildPath "Server"
$databaseProjectPath = Join-Path $source_dir "Database"
$projectConfig = $env:BuildConfiguration
$framework = "net9.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Update" }

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-Path $source_dir "Database" "scripts"

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
		Log-Message -Message "Running on Linux" -Type "INFO"
		if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
		if (Test-IsDockerRunning) {
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
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
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
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "IntegrationTests") --no-build `
				--no-restore --configuration $projectConfig `
				--collect:"XPlat Code Coverage"
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
	$playwrightScript = Join-Path "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		Log-Message -Message "Playwright script found at $playwrightScript." -Type "INFO"
		
		# Check if browsers are installed
		try {
			$listOutput = & pwsh $playwrightScript list 2>&1 | Out-String
			if ($listOutput -match "chromium|webkit|firefox") {
				Log-Message -Message "Playwright browsers are installed." -Type "INFO"
			}
			else {
				Log-Message -Message "WARNING: Playwright browsers may not be installed. Run 'pwsh $playwrightScript install --with-deps' to install them." -Type "WARN"
			}
		}
		catch {
			Log-Message -Message "WARNING: Could not verify Playwright browser installation. Run 'pwsh $playwrightScript install --with-deps' if tests fail." -Type "WARN"
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
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
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
	$databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	
	if (Test-IsLinux) {
		$containerName = Get-ContainerName -DatabaseName $databaseNameFunc
		$sqlPassword = "${containerName}#1A"
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
	
	New-DockerContainerForSqlServer -containerName $(Get-ContainerName $tempDatabaseName)
	New-SqlServerDatabase -serverName $serverName -databaseName $tempDatabaseName 

	Update-AppSettingsConnectionStrings -databaseNameToUse $tempDatabaseName -serverName $serverName -sourceDir $source_dir
	$databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	$dbArgs = @($databaseDll, $dbAction, $serverName, $tempDatabaseName, $scriptDir, "sa", $tempDatabaseName)
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
		throw "Package file not found"
	}
	
	Log-Message -Message "Publishing $($packageFile.Name) to GitHub Packages..." -Type "INFO"
	exec {
		& dotnet nuget push $packageFile.FullName --source $githubFeed --api-key $githubToken --skip-duplicate
	}
	Log-Message -Message "Successfully published $($packageFile.Name) to GitHub Packages" -Type "INFO"
}

Function PackageUI {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $(Join-Path $uiProjectPath "bin" $projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}
	
	# Log package creation (publishing handled separately)
	if (Test-IsGitHubActions) {
		Log-Message -Message "Would publish $projectName.UI.$version.nupkg to GitHub Packages" -Type "INFO"
	}
	elseif (Test-IsAzureDevOps) {
		# Azure DevOps pipeline handles publishing via separate task
		Log-Message -Message "Package ready for Azure DevOps Artifacts publishing" -Type "INFO"
	}
}

Function PackageDatabase {    
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}
	
	# Log package creation (publishing handled separately)
	if (Test-IsGitHubActions) {
		Log-Message -Message "Would publish $projectName.Database.$version.nupkg to GitHub Packages" -Type "INFO"
	}
	elseif (Test-IsAzureDevOps) {
		Log-Message -Message "Package ready for Azure DevOps Artifacts publishing" -Type "INFO"
	}
}

Function PackageAcceptanceTests {  
	# Use Debug configuration so full symbols are available to display better error messages in test failures
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $(Join-Path $acceptanceTestProjectPath "bin" "Debug" $framework "publish") --outFolder $build_dir --overwrite
	}
	
	# Log package creation (publishing handled separately)
	if (Test-IsGitHubActions) {
		Log-Message -Message "Would publish $projectName.AcceptanceTests.$version.nupkg to GitHub Packages" -Type "INFO"
	}
	elseif (Test-IsAzureDevOps) {
		Log-Message -Message "Package ready for Azure DevOps Artifacts publishing" -Type "INFO"
	}
}

Function PackageScript {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}
	
	# Log package creation (publishing handled separately)
	if (Test-IsGitHubActions) {
		Log-Message -Message "Would publish $projectName.Script.$version.nupkg to GitHub Packages" -Type "INFO"
	}
	elseif (Test-IsAzureDevOps) {
		Log-Message -Message "Package ready for Azure DevOps Artifacts publishing" -Type "INFO"
	}
}


Function Package-Everything{
	if (Test-IsAzureDevOps) {
		Write-Output "Packaging nuget packages for Azure DevOps Artifacts"
	}
	elseif (Test-IsGitHubActions) {
		Write-Output "Packaging nuget packages for GitHub Packages"
	}
	else {
		Write-Output "Packaging nuget packages"
	}
	
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

Function Run-AcceptanceTests {
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
	if (-not [string]::IsNullOrEmpty($databaseName)) {
		$script:databaseName = $databaseName
	}
	else {
		if (Test-IsLinux) {
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
	MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	AcceptanceTests
	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir

	$sw.Stop()
	Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}

Function PrivateBuild {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = ""
	)
	
	Log-Message -Message "Starting PrivateBuild..." -Type "INFO"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	
	# Set database server from parameter if provided
	if (-not [string]::IsNullOrEmpty($databaseServer)) {
		$script:databaseServer = $databaseServer
		Log-Message -Message "Using database server from parameter: $script:databaseServer" -Type "INFO"
	}
	else {
		$script:databaseServer = ""
	}
	
	# Generate unique database name for this build instance
	# On Linux with Docker, no need for unique names since container is clean
	if (Test-IsLinux) {
		$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $false
	}
	else {
		$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
	}

	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	Init
	Compile
	UnitTests
	
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
	
	IntegrationTest

	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
	
	$sw.Stop()
	Log-Message -Message "PRIVATE BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}

Function CIBuild {
	if (Test-IsAzureDevOps) {
		Log-Message -Message "Starting CIBuild on Azure DevOps..." -Type "INFO"
	}
	elseif (Test-IsGitHubActions) {
		Log-Message -Message "Starting CIBuild on GitHub Actions..." -Type "INFO"
	}
	else {
		Log-Message -Message "Starting CIBuild..." -Type "INFO"
	}
	
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate database name based on environment
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
	
	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
	
	Package-Everything
	$sw.Stop()
	Log-Message -Message "CIBUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}