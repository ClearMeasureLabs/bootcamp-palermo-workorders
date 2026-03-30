#!/usr/bin/env pwsh
. .\BuildFunctions.ps1

# Clean environment variables that may interfere with local builds
if ($env:ConnectionStrings__SqlConnectionString -and -not (Test-IsGitHubActions)) {
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

if (Test-IsArmArchitecture) {
	$env:database_engine = "SQLite"
	$env:DATABASE_ENGINE = "SQLite"
}
$script:databaseEngine = $env:DATABASE_ENGINE

$databaseName = $projectName

$script:databaseServer = $databaseServer;
$script:databaseScripts = Join-PathSegments $source_dir "Database" "scripts"

if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0" }
if ([string]::IsNullOrEmpty($projectConfig)) { $projectConfig = "Release" }

# ── Main Functions ──────────────────────────────────────────────────────────────

Function Init {
	$pwshPath = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
	if (-not $pwshPath) {
		throw "PowerShell 7 is required to run this build script."
	}

	Initialize-SqlServerModule

	if (Test-IsLinux) {
		if (-not (Test-IsGitHubActions)) {
			$env:NUGET_PACKAGES = "/tmp/nuget-packages"
		}
	}

	if ([string]::IsNullOrEmpty($script:databaseServer)) {
		$script:databaseServer = Get-DefaultDatabaseServer -engine $script:databaseEngine
	}

	switch ($script:databaseEngine) {
		"SQL-Container" {
			if (-not (Test-IsDockerRunning)) {
				throw "Docker is not running. Start Docker (for example, Docker Desktop) so the container-based SQL Server required for 'SQL-Container' builds can run, then rerun this build script."
			}
		}
		"SQLite" {
			if ([string]::IsNullOrEmpty($env:ConnectionStrings__SqlConnectionString)) {
				$env:ConnectionStrings__SqlConnectionString = "Data Source=ChurchBulletin.db"
			}
		}
	}

	if (Test-Path "build") {
		Remove-Item -Path "build" -Recurse -Force
	}

	New-Item -Path $build_dir -ItemType Directory -Force | Out-Null

	exec {
		& dotnet clean $solutionName -nologo -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}

	exec {
		& dotnet restore $solutionName -nologo --interactive -v $verbosity /p:SuppressNETCoreSdkPreviewMessage=true
	}
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

Function Setup-DatabaseForBuild {
	if ($script:databaseEngine -eq "SQL-Container") {
		New-DockerContainerForSqlServer -containerName $(Get-ContainerName $script:databaseName)
		New-SqlServerDatabase -serverName $script:databaseServer -databaseName $script:databaseName
		$containerName = Get-ContainerName -DatabaseName $script:databaseName
		$sqlPassword = Get-SqlServerPassword -ContainerName $containerName
		$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $script:databaseServer -database $script:databaseName -password $sqlPassword
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
	elseif ($script:databaseEngine -eq "LocalDB") {
		$env:ConnectionStrings__SqlConnectionString = New-IntegratedConnectionString -server $script:databaseServer -database $script:databaseName
		MigrateDatabaseLocal -databaseServerFunc $script:databaseServer -databaseNameFunc $script:databaseName
	}
}

Function IntegrationTest {
	Push-Location -Path $integrationTestProjectPath

	try {
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

Function Package-Everything{

	# Allow Octopus.DotNet.Cli (targets net6.0) to run on the current .NET SDK
	$env:DOTNET_ROLL_FORWARD = "LatestMajor"

	dotnet tool install --global Octopus.DotNet.Cli 2>$null # prevents red 'already installed' message

	# Ensure dotnet tools are in PATH
	$dotnetToolsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile), ".dotnet", "tools")
	$pathEntries = $env:PATH -split [System.IO.Path]::PathSeparator
	$dotnetToolsPathPresent = $pathEntries | Where-Object { $_.Trim().ToLowerInvariant() -eq $dotnetToolsPath.Trim().ToLowerInvariant() }
	if (-not $dotnetToolsPathPresent) {
		$env:PATH = "$dotnetToolsPath$([System.IO.Path]::PathSeparator)$env:PATH"
	}

	PackageUI
	PackageDatabase
	PackageAcceptanceTests
	PackageScript
}

Function Build {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",

		[Parameter(Mandatory = $false)]
		[string]$databaseName = "",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)

	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
		}
		$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
	}

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	Init
	Compile
	UnitTests
	Setup-DatabaseForBuild
	IntegrationTest

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
	}
}

Function Invoke-CIBuild {
	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		$script:databaseName = Get-ResolvedDatabaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild $false
	}

	$script:buildStopwatch = [Diagnostics.Stopwatch]::StartNew()

	Init
	Compile
	UnitTests
	Setup-DatabaseForBuild
	IntegrationTest

	$script:buildStopwatch.Stop()
	$elapsed = $script:buildStopwatch.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
	}
}

# ── Helper Functions (in call order) ────────────────────────────────────────────

Function Resolve-DatabaseEngine {
	$onLinux = Test-IsLinux
	$dockerAvailable = $false
	if ($onLinux) {
		$dockerAvailable = Test-IsDockerRunning
	}
	$script:databaseEngine = Get-ResolvedDatabaseEngine -currentEngine $script:databaseEngine -onLinux $onLinux -dockerAvailable $dockerAvailable
	$script:useSqlite = ($script:databaseEngine -eq "SQLite")
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

Function PackageUI {
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
		Copy-Item -Path $playwrightSource -Destination (Join-Path $publishDir ".playwright") -Recurse -Force
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

Function AcceptanceTests {
	$projectConfig = "Release"
	Push-Location -Path $acceptanceTestProjectPath

	$playwrightScript = Join-PathSegments "bin" "Release" $framework "playwright.ps1"

	if (Test-Path $playwrightScript) {
		& pwsh $playwrightScript install chromium --with-deps
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to install Playwright chromium"
		}
	}
	else {
		throw "Playwright script not found at $playwrightScript. Cannot run acceptance tests without the browsers."
	}

	$uiServerProcess = Get-Process -Name "ClearMeasure.Bootcamp.UI.Server" -ErrorAction SilentlyContinue
	if ($uiServerProcess) {
		Log-Message -Message "Warning: ClearMeasure.Bootcamp.UI.Server is already running in background (PID: $($uiServerProcess.Id)). This may interfere with acceptance tests." -Type "WARNING"
	}

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

Function Invoke-AcceptanceTests {
	param (
		[Parameter(Mandatory = $false)]
		[string]$databaseServer = "",
		[Parameter(Mandatory=$false)]
		[string]$databaseName ="",

		[Parameter(Mandatory = $false)]
		[switch]$UseSqlite
	)

	$projectConfig = "Release"
	$sw = [Diagnostics.Stopwatch]::StartNew()

	if ($UseSqlite) {
		$script:databaseEngine = "SQLite"
	}

	Resolve-DatabaseEngine

	if ($script:databaseEngine -ne "SQLite") {
		if (-not [string]::IsNullOrEmpty($databaseServer)) {
			$script:databaseServer = $databaseServer
		}
		$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName -baseName $projectName -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
	}

	Init
	Compile
	Setup-DatabaseForBuild
	AcceptanceTests

	$sw.Stop()
	$elapsed = $sw.Elapsed.ToString()
	if ($script:databaseEngine -eq "SQLite") {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED (SQLite) - Build time: $elapsed" -Type "INFO"
	}
	else {
		Log-Message -Message "ACCEPTANCE BUILD SUCCEEDED - Build time: $elapsed" -Type "INFO"
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
	$tempDatabaseName = Generate-UniqueDatabaseName -baseName $projectName -generateUnique $true
	$containerName = Get-ContainerName -DatabaseName $tempDatabaseName
	$sqlPassword = Get-SqlServerPassword -ContainerName $containerName

	New-DockerContainerForSqlServer -containerName $containerName
	New-SqlServerDatabase -serverName $serverName -databaseName $tempDatabaseName

	$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $serverName -database $tempDatabaseName -password $sqlPassword
	$databaseDll = Join-PathSegments $source_dir "Database" "bin" $projectConfig $framework "ClearMeasure.Bootcamp.Database.dll"
	$dbArgs = @($databaseDll, $dbAction, $serverName, $tempDatabaseName, $scriptDir, "sa", $sqlPassword)
	& dotnet $dbArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Database migration failed with exit code $LASTEXITCODE"
	}
	# Restore connection string to default project database
	$env:ConnectionStrings__SqlConnectionString = New-SqlServerConnectionString -server $script:databaseServer -database $projectName -password (Get-SqlServerPassword -ContainerName (Get-ContainerName -DatabaseName $projectName))
}
