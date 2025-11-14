. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString) {
	Log-Message -Message "Clearing ConnectionStrings__SqlConnectionString environment variable" -Type "INFO"
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

$verbosity = "minimal"

$build_dir = Join-Path $base_dir "build"
$test_dir = Join-Path $build_dir "test"
$solutionName = Join-Path $source_dir "$projectName.sln"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild" }

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

if ($IsLinux) {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
}
else {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }
}
$script:databaseServer = $databaseServer

$databaseScripts = Join-Path $source_dir "Database" "scripts"

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

	$resultsDir = Join-Path $test_dir "UnitTests"
	try {
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $resultsDir --no-build `
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

	$resultsDir = Join-Path $test_dir "IntegrationTests"
	try {
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $resultsDir --no-build `
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

	pwsh bin/Debug/$framework/playwright.ps1 install 

	$resultsDir = Join-Path $test_dir "AcceptanceTests"
	try {
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $resultsDir --no-build `
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

	Log-Message -Message "Migrating database '$databaseNameFunc' on server '$databaseServerFunc' with project '$dbProjectName'" -Type "INFO"

	exec {
		& dotnet run --project $dbProjectName --no-build --verbosity $verbosity --configuration $projectConfig -- $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts
	}
}

Function PackageUI {    

	$packagePath = Join-Path $uiProjectPath "bin" $projectConfig $framework "publish"
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $packagePath --outFolder $build_dir  --overwrite
	}
}

Function PackageDatabase {    
	# Publish as a single-file executable
	$databasePublishPath = Join-Path $databaseProjectPath "bin" $projectConfig $framework "publish"
	exec {
		& dotnet publish $databaseProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig `
			/p:PublishSingleFile=true /p:SelfContained=false /p:IncludeNativeLibrariesForSelfExtract=true
	}
	
	# Copy scripts folder to publish directory
	$scriptsSource = Join-Path $databaseProjectPath "scripts"
	$scriptsDestination = Join-Path $databasePublishPath "scripts"
	if (Test-Path $scriptsSource) {
		Copy-Item -Path $scriptsSource -Destination $scriptsDestination -Recurse -Force
		Log-Message -Message "Copied scripts folder to publish directory" -Type "INFO"
	}
	
	# Package the published output
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databasePublishPath --outFolder $build_dir --overwrite
	}
}

Function PackageAcceptanceTests {  
	# Use Debug configuration so full symbols are available to display better error messages in test failures

	$acceptanceProjectPath  = Join-Path $acceptanceTestProjectPath "bin" "Debug" $framework "publish"
	exec {
		& dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
	}
	exec {
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $acceptanceProjectPath --outFolder $build_dir --overwrite
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


Function Package-Everything {
	Log-Message -Message "Packaging nuget packages" -Type "INFO"
	dotnet tool install --global Octopus.DotNet.Cli | Out-Null
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild {

	Log-Message -Message "Starting Private Build..." -Type "INFO"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate unique database name for this build instance
	$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName
	
	Init
	Compile
	UnitTests
	
	
	if (Test-IsLinux) 
	{
		Log-Message -Message "Setting up SQL Server in Docker" -Type "INFO"
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			New-DockerContainerForSqlServer -databaseName $script:databaseName
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

	$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName

	Init
	Compile
	UnitTests

	if (Test-IsLinux) 
	{
		Log-Message -Message "Setting up SQL Server in Docker" -Type "INFO"
		# For Linux, can't use LocalDB, so spin SQL Server in Docker.
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			Log-Message -Message "Standing up SQL Server in Docker for Linux environment" -Type "INFO"
			New-DockerContainerForSqlServer -databaseName $script:databaseName
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
}

Function Invoke-Build {
	<#
	.SYNOPSIS
		Invokes the appropriate build based on the environment
	.DESCRIPTION
		Detects whether the script is running in Azure DevOps or locally and executes the corresponding build function.
		In Azure DevOps, runs CIBuild which includes packaging.
		In local environments, runs PrivateBuild which uses a unique database name for isolation.
	.EXAMPLE
		Invoke-Build
	#>

	if (Test-IsAzureDevOps) {
		CIBuild
	}
	else {
		PrivateBuild
	}
}