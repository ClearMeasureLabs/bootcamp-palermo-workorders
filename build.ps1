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


$aliaSql = "$source_dir\Database\scripts\AliaSql.exe"

$databaseAction = $env:DatabaseAction
if ([string]::IsNullOrEmpty($databaseAction)) { $databaseAction = "Rebuild"}

$databaseName = $projectName
if ([string]::IsNullOrEmpty($databaseName)) { $databaseName = $projectName}

$script:databaseServer = $databaseServer
if ([string]::IsNullOrEmpty($script:databaseServer)) { $script:databaseServer = "(LocalDb)\MSSQLLocalDB"}

$script:databaseMode = $databaseMode
if ([string]::IsNullOrEmpty($script:databaseMode)) { $script:databaseMode = "sqllocaldb" }

$databaseScripts = "$source_dir\Database\scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0"}
if ([string]::IsNullOrEmpty($projectConfig)) {$projectConfig = "Release"}

Function Generate-UniqueDatabaseName {
    param (
        [Parameter(Mandatory=$true)]
        [string]$baseName
    )
    
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $randomChars = -join ((65..90) + (97..122) | Get-Random -Count 4 | ForEach-Object {[char]$_})
    $uniqueName = "${baseName}_${timestamp}_${randomChars}"
 
    Write-Host "Generated unique database name: $uniqueName" -ForegroundColor Cyan
    return $uniqueName
}
 
Function Init {
	# Check for PowerShell 7
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source

	if (-not $pwshPath) {
		Write-Warning "PowerShell 7 is not installed. Please install it from https://aka.ms/powershell"
	} else {
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

Function AcceptanceTests{
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
	 [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseServerFunc,
		
	    [Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$databaseNameFunc,

		[string]$username = $null,
		[string]$password = $null
	)
	if ($username -and $password) {
		exec{
			& $aliaSql $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts $username $password
		}
	} else {
		exec{
			& $aliaSql $databaseAction $databaseServerFunc $databaseNameFunc $databaseScripts
		}
	}
}

Function PackageUI {    
    exec{
      & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
	exec{
		& dotnet-octo pack --id "$projectName.UI" --version $version --basePath $uiProjectPath\bin\$projectConfig\$framework\publish --outFolder $build_dir  --overwrite
	}
}

Function PackageDatabase {    
    exec{
		& dotnet-octo pack --id "$projectName.Database" --version $version --basePath $databaseProjectPath --outFolder $build_dir --overwrite
	}
}

Function PackageAcceptanceTests {  
    # Use Debug configuration so full symbols are available to display better error messages in test failures
    exec{
        & dotnet publish $acceptanceTestProjectPath -nologo --no-restore -v $verbosity --configuration Debug
    }
	exec{
		& dotnet-octo pack --id "$projectName.AcceptanceTests" --version $version --basePath $acceptanceTestProjectPath\bin\Debug\$framework\publish --outFolder $build_dir --overwrite
	}
}

Function PackageScript {    
    exec{
        & dotnet publish $uiProjectPath -nologo --no-restore --no-build -v $verbosity --configuration $projectConfig
    }
	exec{
		& dotnet-octo pack --id "$projectName.Script" --version $version --basePath $uiProjectPath --include "*.ps1" --outFolder $build_dir  --overwrite
	}
}


Function Package{
	Write-Output "Packaging nuget packages"
	dotnet tool install --global Octopus.DotNet.Cli | Write-Output $_ -ErrorAction SilentlyContinue #prevents red color is already installed
    PackageUI
    PackageDatabase
    PackageAcceptanceTests
	PackageScript
}

Function Start-SqlContainer {
	$containerName = "privatebuild-sqlserver"
	$saPassword = "P@ssw0rd!Build2024"
	$port = 11433
	
	# Stop and remove existing container if present
	docker rm -f $containerName 2>$null
	
	Write-Host "Starting SQL Server container: $containerName on port $port" -ForegroundColor Cyan
	exec {
		docker run -d --name $containerName `
			-e "ACCEPT_EULA=Y" `
			-e "MSSQL_SA_PASSWORD=$saPassword" `
			-p "${port}:1433" `
			mcr.microsoft.com/mssql/server:2022-latest
	}
	
	# Wait for SQL Server to be ready
	Write-Host "Waiting for SQL Server container to be ready..." -ForegroundColor Yellow
	$maxRetries = 30
	$retryCount = 0
	$ready = $false
	while (-not $ready -and $retryCount -lt $maxRetries) {
		Start-Sleep -Seconds 3
		$retryCount++
		try {
			$result = docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $saPassword -C -Q "SELECT 1" 2>$null
			if ($LASTEXITCODE -eq 0) {
				$ready = $true
				Write-Host "SQL Server container is ready after $($retryCount * 3) seconds" -ForegroundColor Green
			} else {
				Write-Host "  Retry $retryCount/$maxRetries..." -ForegroundColor Gray
			}
		} catch {
			Write-Host "  Retry $retryCount/$maxRetries..." -ForegroundColor Gray
		}
	}
	
	if (-not $ready) {
		throw "SQL Server container did not become ready within $($maxRetries * 3) seconds"
	}
	
	$script:databaseServer = "localhost,$port"
	$script:sqlContainerName = $containerName
	$script:sqlContainerPassword = $saPassword
}

Function Stop-SqlContainer {
	if ($script:sqlContainerName) {
		Write-Host "Stopping SQL Server container: $($script:sqlContainerName)" -ForegroundColor Cyan
		docker rm -f $script:sqlContainerName 2>$null
	}
}

Function Update-AppSettingsForSqlite {
	param (
		[Parameter(Mandatory=$true)]
		[string]$sqliteDbPath,
		[Parameter(Mandatory=$true)]
		[string]$sourceDir
	)
	
	$connectionString = "Data Source=$sqliteDbPath"
	Write-Host "Configuring SQLite mode with connection string: $connectionString" -ForegroundColor Cyan
	
	$env:ConnectionStrings__SqlConnectionString = $connectionString
	$env:DatabaseProvider = "Sqlite"
	
	$appSettingsFiles = Get-ChildItem -Path $sourceDir -Recurse -Filter "appsettings*.json"
	
	foreach ($file in $appSettingsFiles) {
		Write-Host "Processing file: $($file.FullName)" -ForegroundColor Gray
		$content = Get-Content $file.FullName -Raw | ConvertFrom-Json
		
		if ($content.PSObject.Properties.Name -contains "ConnectionStrings") {
			$content.ConnectionStrings.SqlConnectionString = $connectionString
			Write-Host "  Updated SqlConnectionString: $connectionString" -ForegroundColor Green
		}
		
		# Add or update DatabaseProvider
		if ($content.PSObject.Properties.Name -contains "DatabaseProvider") {
			$content.DatabaseProvider = "Sqlite"
		} else {
			$content | Add-Member -NotePropertyName "DatabaseProvider" -NotePropertyValue "Sqlite" -Force
		}
		
		$content | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
	}
}

Function Update-AppSettingsForSqlContainer {
	param (
		[Parameter(Mandatory=$true)]
		[string]$databaseNameToUse,
		[Parameter(Mandatory=$true)]
		[string]$serverName,
		[Parameter(Mandatory=$true)]
		[string]$password,
		[Parameter(Mandatory=$true)]
		[string]$sourceDir
	)
	
	$connectionString = "server=$serverName;database=$databaseNameToUse;User Id=sa;Password=$password;TrustServerCertificate=true;"
	Write-Host "Configuring SQL Container mode with connection string: $connectionString" -ForegroundColor Cyan
	
	$env:ConnectionStrings__SqlConnectionString = $connectionString
	if ($env:DatabaseProvider) { Remove-Item Env:\DatabaseProvider -ErrorAction SilentlyContinue }
	
	$appSettingsFiles = Get-ChildItem -Path $sourceDir -Recurse -Filter "appsettings*.json"
	
	foreach ($file in $appSettingsFiles) {
		Write-Host "Processing file: $($file.FullName)" -ForegroundColor Gray
		$content = Get-Content $file.FullName -Raw | ConvertFrom-Json
		
		if ($content.PSObject.Properties.Name -contains "ConnectionStrings") {
			$content.ConnectionStrings.SqlConnectionString = $connectionString
			Write-Host "  Updated SqlConnectionString: $connectionString" -ForegroundColor Green
		}
		
		# Remove DatabaseProvider if set (SQL Server is default)
		if ($content.PSObject.Properties.Name -contains "DatabaseProvider") {
			$content.PSObject.Properties.Remove("DatabaseProvider")
		}
		
		$content | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
	}
}

Function Restore-AppSettingsDefaults {
	param (
		[Parameter(Mandatory=$true)]
		[string]$sourceDir
	)
	
	$defaultServer = "(LocalDb)\MSSQLLocalDB"
	$defaultDb = "ChurchBulletin"
	$defaultConnStr = "server=$defaultServer;database=$defaultDb;Integrated Security=true;"
	
	if ($env:DatabaseProvider) { Remove-Item Env:\DatabaseProvider -ErrorAction SilentlyContinue }
	$env:ConnectionStrings__SqlConnectionString = $null
	
	$appSettingsFiles = Get-ChildItem -Path $sourceDir -Recurse -Filter "appsettings*.json"
	
	foreach ($file in $appSettingsFiles) {
		$content = Get-Content $file.FullName -Raw | ConvertFrom-Json
		
		if ($content.PSObject.Properties.Name -contains "ConnectionStrings") {
			$content.ConnectionStrings.SqlConnectionString = $defaultConnStr
		}
		
		if ($content.PSObject.Properties.Name -contains "DatabaseProvider") {
			$content.PSObject.Properties.Remove("DatabaseProvider")
		}
		
		$content | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
	}
	
	Write-Host "Restored appsettings to defaults" -ForegroundColor Cyan
}

Function PrivateBuild{
	param (
		[string]$mode = $script:databaseMode
	)

	$projectConfig = "Debug"
	[Environment]::SetEnvironmentVariable("containerAppURL", "localhost:7174", "User")
	$sw = [Diagnostics.Stopwatch]::StartNew()
	
	Write-Host "=== PRIVATE BUILD - Database Mode: $mode ===" -ForegroundColor Magenta
	
	Init
	Compile
	UnitTests
	
	switch ($mode.ToLower()) {
		"sqlite" {
			$sqliteDbPath = "$base_dir\build\privatebuild_test.db"
			if (Test-Path $sqliteDbPath) { Remove-Item $sqliteDbPath -Force }
			
			Update-AppSettingsForSqlite -sqliteDbPath $sqliteDbPath -sourceDir $source_dir
			
			# SQLite uses EF Core EnsureCreated - no AliaSQL needed
			Write-Host "SQLite mode: database will be created by EF Core EnsureCreated" -ForegroundColor Cyan
			
			IntegrationTest
			
			Restore-AppSettingsDefaults -sourceDir $source_dir
		}
		"sqlcontainer" {
			Start-SqlContainer
			
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName
			
			Update-AppSettingsForSqlContainer -databaseNameToUse $script:databaseName `
				-serverName $script:databaseServer `
				-password $script:sqlContainerPassword `
				-sourceDir $source_dir
			
			MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName -username "sa" -password $script:sqlContainerPassword
			
			IntegrationTest
			
			Restore-AppSettingsDefaults -sourceDir $source_dir
			Stop-SqlContainer
		}
		default {
			# sqllocaldb mode (original behavior)
			$script:databaseName = Generate-UniqueDatabaseName -baseName $projectName
			
			Update-AppSettingsConnectionStrings -databaseNameToUse $script:databaseName -serverName $script:databaseServer -sourceDir $source_dir
			
			MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
			
			IntegrationTest

			Update-AppSettingsConnectionStrings -databaseNameToUse $projectName -serverName $script:databaseServer -sourceDir $source_dir
		}
	}
	
	$sw.Stop()
	write-host "BUILD SUCCEEDED [$mode] - Build time: " $sw.Elapsed.ToString() -ForegroundColor Green
	if ($script:databaseName) {
		write-host "Database used: $script:databaseName" -ForegroundColor Cyan
	}
}

Function CIBuild{
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