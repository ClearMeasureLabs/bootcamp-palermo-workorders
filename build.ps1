param(
    [string]$flywayCliDir = "$base_dir\flyway"
)

. .\BuildFunctions.ps1
. .\FlywayFunctions.ps1

$projectName = "ChurchBulletin"
$base_dir = resolve-path .\
$source_dir = "$base_dir\src"
$unitTestProjectPath = "$source_dir\UnitTests"
$integrationTestProjectPath = "$source_dir\IntegrationTests"
$acceptanceTestProjectPath = "$source_dir\AcceptanceTests"
$uiProjectPath = "$source_dir\UI\Server"
$databaseProjectPath = "$source_dir\Database"
$databaseFlywayProjectPath = "$source_dir\DatabaseFlyway"
$mauiProjectPath = "$source_dir\UI\Maui"
$projectConfig = $env:BuildConfiguration
$framework = "net8.0"
$version = $env:BUILD_BUILDNUMBER

$verbosity = "minimal"

$build_dir = "$base_dir\build"
$test_dir = "$build_dir\test"


$aliaSql = "$source_dir\Database\scripts\AliaSql.exe"
$flywayCli = "$flywayCliDir\flyway.cmd"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild"}

#We will create 3x databases for Flyway
$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName}

$devDatabaseName = $databaseName + "_Dev"
$shadowDatabaseName = $databaseName + "_Shadow"

#We may need three separate servers, but for initialization and/or testing, just use the same, root SQL Server for the 3x test Flyway dbs.
$script:databaseServer = $databaseServer
if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB"}

$script:devDatabaseServer = $databaseServer
$script:shadowDatabaseServer = $databaseServer


$databaseScripts = "$source_dir\Database\scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0"}
if ([string]::IsNullOrEmpty($projectConfig)) {$projectConfig = "Release"}
 
Function Init {
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

Function Compile{
	exec {
		& dotnet build $source_dir\$projectName.sln -nologo --no-restore -v `
			$verbosity -maxcpucount --configuration $projectConfig --no-incremental `
			/p:TreatWarningsAsErrors="true" `
			/p:Version=$version /p:Authors="Programming with Palermo" `
			/p:Product="Church Bulletin"
	}
}

Function UnitTests{
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

Function IntegrationTest{
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

Function MigrateDatabaseLocal {
	param (
	    [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseServerFunc,
		
	    [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseNameFunc
	)
	exec{
		& $aliaSql $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts
	}
	
	if ($migrateDbWithFlyway) {
		#call 'flyaway migrate' with db parameters
		$migrationsPath = "$databaseFlywayProjectPath\migrations"
		$flywayParameters = @(
			"-configFiles=$databaseFlywayProjectPath\flyway.toml"
			"-url=jdbc:sqlserver://$databaseServerFunc;databaseName=$databaseNameFunc;encrypt=false;integratedSecurity=true;trustServerCertificate=true"
			"-locations=filesystem:$migrationsPath"
			"migrate"
		)

		exec {
				& $flywayCli $flywayParameters
		}
	}
}

Function PackageUI {    
    exec{
        & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
	exec{
		& octopus package nuget create --id "$projectName.UI" --version $version --base-path $uiProjectPath\bin\$projectConfig\$framework\publish --out-folder $build_dir  --overwrite
	}
}

Function PackageDatabase {    
    exec{
		& octopus package nuget create --id "$projectName.Database" --version $version --base-path $databaseProjectPath --out-folder $build_dir --overwrite
	}
}

Function PackageDatabaseFlyway {    
    exec{
		& octopus package nuget create --id "$projectName.DatabaseFlyway" --version $version --base-path $databaseFlywayProjectPath --out-folder $build_dir --overwrite
	}
}

Function PackageAcceptanceTests {       
    # Use Debug configuration so full symbols are available to display better error messages in test failures
    exec{
        & dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
    }
	exec{
		& octopus package nuget create --id "$projectName.AcceptanceTests" --version $version --base-path $acceptanceTestProjectPath\bin\Debug\$framework\publish --out-folder $build_dir --overwrite
	}
}

Function PackageScript {    
    exec{
        & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
	exec{
		& octopus package nuget create --id "$projectName.Script" --version $version --base-path $uiProjectPath --include "*.ps1" --out-folder $build_dir  --overwrite
	}
}


Function Package{
	Write-Output "Packaging nuget packages"
    PackageUI
    PackageDatabase
    PackageDatabaseFlyway
    PackageAcceptanceTests
	PackageScript
}

Function PrivateBuild{
	$projectConfig = "Debug"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	Init
	Compile
	UnitTests
	
	#We need 3 databases for 
	MigrateDatabaseLocal -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
	
	#These are extra Dbs that are can be part of a Flyway setup
	if ($migrateDbWithFlyway) {
		MigrateDatabaseLocal -databaseServerFunc $devDatabaseServer -databaseNameFunc $devDatabaseName
		MigrateDatabaseLocal -databaseServerFunc $shadowDatabaseServer -databaseNameFunc $shadowDatabaseName
	}
	IntegrationTest
	
	$sw.Stop()
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
}

Function CIBuild{
	$sw = [Diagnostics.Stopwatch]::StartNew()
	Init
	Compile
	UnitTests
	MigrateDatabaseLocal  -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
	IntegrationTest
	Package
	$sw.Stop()
	write-host "BUILD SUCCEEDED - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
}