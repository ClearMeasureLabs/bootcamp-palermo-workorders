. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString) {
	Write-Host "Clearing ConnectionStrings__SqlConnectionString environment variable"
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

if (Test-IsLinux) {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
}
else {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }
}
$script:databaseServer = $databaseServer

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
		if (Test-IsDockerRunning) {
			Log-Message -Message "Docker is running" -Type "INFO"
		} else {
			Log-Message -Message "Docker is not running. Please start Docker to run SQL Server in a container." -Type "ERROR"
			throw "Docker is not running."
		}
	}
	elseif (Test-IsWindows) {
		Log-Message -Message "Running on Windows" -Type "INFO"
	}

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
	$projectConfig = "Debug"
	Push-Location -Path $acceptanceTestProjectPath

	pwsh (Join-Path "bin" "Debug" $framework "playwright.ps1") install --with-deps

	try {
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $(Join-Path $test_dir "AcceptanceTests") --no-build `
				--no-restore --configuration $projectConfig `
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
	Log-Message "Creating SQL Server in Docker for integration tests for $tempDatabaseName" -Type "INFO"
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

Function PackageUI {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $(Join-Path $uiProjectPath "bin" $projectConfig $framework "publish") --outFolder $build_dir  --overwrite
	}
}

Function PackageDatabase {    
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
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $(Join-Path $acceptanceTestProjectPath "bin" "Debug" $framework "publish") --outFolder $build_dir --overwrite
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
	$dotnetToolsPath = [System.IO.Path]::Combine($env:HOME, ".dotnet", "tools")
	if (-not $env:PATH.Contains($dotnetToolsPath)) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
		Log-Message -Message "Added dotnet tools to PATH: $dotnetToolsPath" -Type "INFO"
	}
	
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild {
	Log-Message -Message "Starting Private Build..." -Type "INFO"
	$projectConfig = "Debug"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate unique database name for this build instance
	# On Linux with Docker, no need for unique names since container is clean
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
	#AcceptanceTests

	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
	
	$sw.Stop()
	Log-Message -Message "BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}

Function CIBuild {
	Log-Message -Message "Starting CI Build..." -Type "INFO"
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
	#AcceptanceTests
	
	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
	
	Package-Everything
	$sw.Stop()
	Log-Message -Message "BUILD SUCCEEDED - Build time: $($sw.Elapsed.ToString())" -Type "INFO"
	Log-Message -Message "Database used: $script:databaseName" -Type "INFO"
}