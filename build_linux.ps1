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
$unitTestProjectPath = Join-Path $source_dir "UnitTests"
$integrationTestProjectPath = Join-Path $source_dir "IntegrationTests"
$acceptanceTestProjectPath = Join-Path $source_dir "AcceptanceTests"
$uiProjectPath = Join-Path $source_dir "UI" "Server"
$databaseProjectPath = Join-Path $source_dir "Database"
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


if (Test-IsLinux) {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "localhost" }
}
else {
	if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }
}
$script:databaseServer = $databaseServer

$databaseScripts = Join-Path $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }


Function Generate-UniqueDatabaseName {
	param (
		[Parameter(Mandatory = $true)]
		[string]$baseName
	)
    
	$timestamp = Get-Date -Format "yyyyMMddHHmmss"
	$randomChars = -join ((65..90) + (97..122) | Get-Random -Count 4 | ForEach-Object { [char]$_ })
	$uniqueName = "${baseName}_${timestamp}_${randomChars}"
 
	Write-Host "Generated unique database name: $uniqueName" -ForegroundColor Cyan
	return $uniqueName
}
 
Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		Write-Warning "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell"
		throw "PowerShell 7 is required to run this build script."
	}
 	else {
		Write-Host "PowerShell 7 found at: $pwshPath"
	}

	if (Test-IsAzureDevOps) { 
		Write-Host "Running in Azure DevOps Pipeline"
	}
	else {
		Write-Host "Running in Local Environment"
	}

	if (Test-IsLinux) {
		Write-Host "Running on Linux"		
		if (Test-IsDockerRunning) {
			Write-Host "Docker is running"
		} else {
			Write-Error "Docker is not running. Please start Docker to run SQL Server in a container."
			throw "Docker is not running."
		}
	}
	elseif (Test-IsWindows) {
		Write-Host "Running on Windows"
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
	
	Write-Output $projectConfig
	Write-Output $version
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

	$dbProjectName = Join-Path $databaseProjectPath "Database.csproj"
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
	exec {
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
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
	Write-Output "Packaging nuget packages"
	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild {

	Write-Host "Starting Private Build..." -ForegroundColor Yellow
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate unique database name for this build instance
	$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName
	
	Init
	Compile
	UnitTests
	
	
	if (Test-IsLinux) 
	{
		write-host "Setting up SQL Server in Docker" -ForegroundColor Cyan
		# For Linux, can't use LocalDB, so spin SQL Server in Docker.
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			Write-Host "Standing up SQL Server in Docker for Linux environment" -ForegroundColor Cyan
			New-DockerContainerForSqlServer -databaseName $script:databaseName
			New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		}
		else {
			Write-Error "Docker is not running. Please start Docker to run SQL Server in a container."
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
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
	write-host "Database used: $script:databaseName" -ForegroundColor Cyan
}

Function CIBuild {

	Write-Host "Starting CI Build..." -ForegroundColor Yellow

	$sw = [Diagnostics.Stopwatch]::StartNew()

	$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName

	Init
	Compile
	UnitTests

	if (Test-IsLinux) 
	{
		write-host "Setting up SQL Server in Docker" -ForegroundColor Cyan
		# For Linux, can't use LocalDB, so spin SQL Server in Docker.
		if (Test-IsDockerRunning -LogOutput $true) 
		{
			Write-Host "Standing up SQL Server in Docker for Linux environment" -ForegroundColor Cyan
			New-DockerContainerForSqlServer -databaseName $script:databaseName
			New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		}
		else {
			Write-Error "Docker is not running. Please start Docker to run SQL Server in a container."
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
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
}

Function Invoke-Build {
	# param (
	# 	[Parameter(Mandatory = $false)]
	# 	[ValidateNotNullOrEmpty()]
	# 	[string]$buildType = "Private"
	# )


	if (Test-IsAzureDevOps) {
		CIBuild
	}
	else {
		PrivateBuild
	}
}