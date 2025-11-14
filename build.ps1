. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString) {
	Write-Host "Clearing ConnectionStrings__SqlConnectionString environment variable"
	$env:ConnectionStrings__SqlConnectionString = $null
	[Environment]::SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", $null, "User")
}

$projectName = "ChurchBulletin"
$base_dir = resolve-path .\
$source_dir = "$base_dir\src"
$unitTestProjectPath = "$source_dir\UnitTests"
$integrationTestProjectPath = "$source_dir\IntegrationTests"
$acceptanceTestProjectPath = "$source_dir\AcceptanceTests"
$uiProjectPath = "$source_dir\UI\Server"
$databaseProjectPath = "$source_dir\Database"
$projectConfig = $env:BuildConfiguration
$framework = "net9.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = "$base_dir\build"
$test_dir = "$build_dir\test"

$aliaSql =  Join-Path "$source_dir\Database\bin\$projectConfig\net9.0" "ClearMeasure.Bootcamp.Database.dll"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild" }

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName }

$script:databaseServer = $databaseServer
if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB" }

$databaseScripts = "$source_dir\Database\scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

 
Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source

	if (-not $pwshPath) {
		Write-Warning "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell"
	}
 else {
		Write-Host "PowerShell 7 found at: $pwshPath"
	}

	& cmd.exe /c rd /S /Q build
	
	mkdir $build_dir > $null

	exec {
		& dotnet clean $source_dir\$projectName.sln -nologo -v $verbosity
	}
	exec {
		& dotnet restore $source_dir\$projectName.sln -nologo --interactive -v $verbosity  
	}
	
	Write-Output $projectConfig
	Write-Output $version
}

Function Compile {
	exec {
		& dotnet build $source_dir\$projectName.sln -nologo --no-restore -v `
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
				--results-directory $test_dir\UnitTests --no-build `
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
				--results-directory $test_dir\IntegrationTests --no-build `
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

	try {
		exec {
			& dotnet test /p:CollectCoverage=true -nologo -v $verbosity --logger:trx `
				--results-directory $test_dir\AcceptanceTests --no-build `
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
		& dotnet $aliaSql $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts
	}
}

Function PackageUI {    
	exec {
		& dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
	}
	exec {
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $uiProjectPath\bin\$projectConfig\$framework\publish --outFolder $build_dir  --overwrite
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
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $acceptanceTestProjectPath\bin\Debug\$framework\publish --outFolder $build_dir --overwrite
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


Function Package {
	Write-Output "Packaging nuget packages"
	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild {
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
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
	write-host "Database used: $script:databaseName" -ForegroundColor Cyan
}

Function CIBuild {
	$sw = [Diagnostics.Stopwatch]::StartNew()
	Init
	Compile
	UnitTests
	MigrateDatabaseLocal  -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
	IntegrationTest
	#AcceptanceTests
	Package
	$sw.Stop()
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
}