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

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild" }

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

$script:databaseServer = $env:DatabaseServer
if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }

$script:databaseScripts = Join-Path $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

 
Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source

	if (-not $pwshPath) {
		Log-Message "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell" -Type "ERROR"
	}
 else {
		Log-Message "PowerShell 7 found at: $pwshPath" -Type "INFO"
	}

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}
	
	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $(Join-Path $source_dir "$projectName.sln") -nologo -v $verbosity
	}
	exec {
		& dotnet restore $(Join-Path $source_dir "$projectName.sln") -nologo --interactive -v $verbosity  
	}
	
	Log-Message "Project Configuration: $projectConfig. Version: $version"
}

Function Compile {
	exec {
		& dotnet build $(Join-Path $source_dir "$projectName.sln") -nologo --no-restore -v `
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
	exec {
		$databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
		& dotnet $databaseDll $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts
	}
}

Function Create-SqlServerInDocker {
	param (
		[Parameter(Mandatory = $true)]
			[ValidateNotNullOrEmpty()]
			[string]$dbAction,
		[Parameter(Mandatory = $true)]
			[ValidateNotNullOrEmpty()]
			[string]$scriptDir			
		)
	$tempDatabaseName = Generate-UniqueDatabaseName -baseName $script:projectName
	Log-Message "Creating SQL Server in Docker for integration tests for $tempDatabaseName" -Type "INFO"
	
	New-DockerContainerForSqlServer -containerName $(Get-ContainerName $tempDatabaseName)
	New-SqlServerDatabase -serverName "localhost" -databaseName $tempDatabaseName 

	Update-AppSettingsConnectionStrings -databaseNameToUse $tempDatabaseName -serverName "localhost" -sourceDir $source_dir
	exec {
		$databaseDll = Join-Path $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
		& dotnet $databaseDll $dbAction "localhost" $tempDatabaseName $scriptDir "sa" $tempDatabaseName
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
	Write-Output "Packaging nuget packages"
	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild {
	Log-Message "Starting Private Build"
	$projectConfig = "Debug"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	# Generate unique database name for this build instance
	$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName
	
	Init
	Compile
	UnitTests
	
	# Update appsettings.json files before database migration
	Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir
	
	MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	
	IntegrationTest
	#AcceptanceTests

	Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
	
	$sw.Stop()
	Log-Message "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -Type "INFO"
	Log-Message "Database used: $script:databaseName" -Type "INFO"
}

Function CIBuild {
	Log-Message
	$sw = [Diagnostics.Stopwatch]::StartNew()
	Init
	Compile
	UnitTests


	if (Test-IsAzureDevOps) 
	{
		Create-SqlServerInDocker $script:databaseAction $script:databaseScripts
	}
	else 
	{
		MigrateDatabaseLocal  -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	IntegrationTest
	#AcceptanceTests
	Package-Everything
	$sw.Stop()
	Log-Message "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -Type "INFO"
}