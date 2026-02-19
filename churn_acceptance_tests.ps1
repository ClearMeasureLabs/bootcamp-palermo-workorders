param(
    [int]$Iterations = 10,
    [int]$LeakGrowthThresholdMB = 50,
    [int]$ConsecutiveGrowthCount = 3,
    [string]$ServerUrl = "https://localhost:7174",
    [string]$Configuration = "Release",
    [string]$TestFilter = "",
    [int]$TestWorkers = 8,
    [switch]$SkipBuild,
    [switch]$ContinueOnTestFailure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path $PSScriptRoot
$serverProject = Join-Path $repoRoot "src\UI\Server\UI.Server.csproj"
$acceptanceProjectDirectory = Join-Path $repoRoot "src\AcceptanceTests"
$acceptanceProject = Join-Path $acceptanceProjectDirectory "AcceptanceTests.csproj"
$runSettings = Join-Path $acceptanceProjectDirectory "AcceptanceTests.runsettings"
$buildDirectory = Join-Path $repoRoot "build"

function Invoke-DotNet {
    param(
        [string[]]$Arguments,
        [string]$WorkingDirectory = $repoRoot
    )

    Push-Location $WorkingDirectory
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-UiServerProcesses {
    $dotnetProcesses = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue
    if (-not $dotnetProcesses) {
        return @()
    }

    return @($dotnetProcesses | Where-Object {
            $_.CommandLine -and $_.CommandLine -match 'UI\.Server(\.dll|\.csproj)?'
        })
}

function Format-MemoryValue {
    param(
        [object]$Value
    )

    if ($null -eq $Value) {
        return "n/a"
    }

    return ("{0:N2}" -f [double]$Value)
}

function Wait-ForServerReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,
        [int]$TimeoutSeconds = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -SkipCertificateCheck -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "UI.Server did not become healthy at $Url within $TimeoutSeconds seconds."
}

function Invoke-AcceptanceTestsWithMemoryMonitor {
    param(
        [string[]]$DotNetTestArguments,
        [int]$ServerProcessId
    )

    Push-Location $acceptanceProjectDirectory
    try {
        $testProcess = Start-Process -FilePath "dotnet" -ArgumentList $DotNetTestArguments `
            -WorkingDirectory $acceptanceProjectDirectory -PassThru -NoNewWindow
        

        $peakPrivateMB = 0.0
        $peakWorkingSetMB = 0.0
        $beforePrivateMB = $null
        $afterPrivateMB = $null
        $samples = 0
        $serverDetected = $false

        while (-not $testProcess.HasExited) {
            $serverProcess = Get-Process -Id $ServerProcessId -ErrorAction SilentlyContinue
            if ($serverProcess) {
                $serverDetected = $true
                $samples++
                $privateMB = [double]$serverProcess.PrivateMemorySize64 / 1MB
                $workingSetMB = [double]$serverProcess.WorkingSet64 / 1MB
                if ($null -eq $beforePrivateMB) {
                    $beforePrivateMB = $privateMB
                }
                $afterPrivateMB = $privateMB
                if ($privateMB -gt $peakPrivateMB) { $peakPrivateMB = $privateMB }
                if ($workingSetMB -gt $peakWorkingSetMB) { $peakWorkingSetMB = $workingSetMB }
            }

            Start-Sleep -Seconds 1
            $testProcess.Refresh()
        }

        $exitCode = $testProcess.ExitCode

        [PSCustomObject]@{
            ExitCode = $exitCode
            ServerDetected = $serverDetected
            PeakPrivateMB = [math]::Round($peakPrivateMB, 2)
            PeakWorkingSetMB = [math]::Round($peakWorkingSetMB, 2)
            BeforePrivateMB = if ($null -eq $beforePrivateMB) { $null } else { [math]::Round($beforePrivateMB, 2) }
            AfterPrivateMB = if ($null -eq $afterPrivateMB) { $null } else { [math]::Round($afterPrivateMB, 2) }
            Samples = $samples
        }
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path $serverProject)) {
    throw "Server project not found: $serverProject"
}

if (-not (Test-Path $acceptanceProject)) {
    throw "Acceptance test project not found: $acceptanceProject"
}

if (-not (Test-Path $runSettings)) {
    throw "Acceptance runsettings not found: $runSettings"
}

if (-not $SkipBuild) {
    Invoke-DotNet -Arguments @("build", $serverProject, "--configuration", $Configuration)
    Invoke-DotNet -Arguments @("build", $acceptanceProject, "--configuration", $Configuration)
}

Push-Location $acceptanceProjectDirectory
try {
    $playwrightScript = Get-ChildItem -Path (Join-Path $acceptanceProjectDirectory "bin\$Configuration") -Filter "playwright.ps1" -Recurse |
        Select-Object -First 1 -ExpandProperty FullName
    if ($playwrightScript) {
        & pwsh $playwrightScript install
        if ($LASTEXITCODE -ne 0) {
            throw "Playwright browser installation failed with exit code $LASTEXITCODE"
        }
    }
}
finally {
    Pop-Location
}

New-Item -Path $buildDirectory -ItemType Directory -Force | Out-Null

$originalStartLocalServer = [Environment]::GetEnvironmentVariable("StartLocalServer", "Process")
$originalApplicationBaseUrl = [Environment]::GetEnvironmentVariable("ApplicationBaseUrl", "Process")
$results = New-Object System.Collections.Generic.List[object]
$consecutiveGrowth = 0
$previousPeakPrivateMB = $null
$baselinePeakPrivateMB = $null
$leakDetected = $false
$serverProcess = $null

try {
    $serverStdOutPath = Join-Path $buildDirectory "churn-server.stdout.log"
    $serverStdErrPath = Join-Path $buildDirectory "churn-server.stderr.log"
    $healthCheckUrl = "$($ServerUrl.TrimEnd('/'))/_healthcheck"

    Write-Host "Starting UI.Server at $ServerUrl..."
    $serverProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
        "run",
        "--project", $serverProject,
        "--configuration", $Configuration,
        "--no-build",
        "--urls", $ServerUrl
    ) -WorkingDirectory $repoRoot -PassThru -NoNewWindow `
        -RedirectStandardOutput $serverStdOutPath -RedirectStandardError $serverStdErrPath

    Wait-ForServerReady -Url $healthCheckUrl

    [Environment]::SetEnvironmentVariable("StartLocalServer", "false", "Process")
    [Environment]::SetEnvironmentVariable("ApplicationBaseUrl", $ServerUrl, "Process")

    for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
        if (-not (Get-Process -Id $serverProcess.Id -ErrorAction SilentlyContinue)) {
            throw "UI.Server process exited unexpectedly before iteration $iteration."
        }

        Write-Host "Iteration $iteration/$Iterations - running acceptance tests..."
        $testArgs = @(
            "test",
            $acceptanceProject,
            "--configuration", $Configuration,
            "--settings", $runSettings,
            "--no-build"
        )
        if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
            $testArgs += @("--filter", $TestFilter)
        }
        $applicationBaseUrlOverride = 'TestRunParameters.Parameter(name=\"ApplicationBaseUrl\",value=\"' + $ServerUrl + '\")'
        $startLocalServerOverride = 'TestRunParameters.Parameter(name=\"StartLocalServer\",value=\"false\")'
        $reloadTestDataOverride = 'TestRunParameters.Parameter(name=\"ReloadTestData\",value=\"false\")'
        $testArgs += @(
            "--",
            $applicationBaseUrlOverride,
            $startLocalServerOverride
        )
        if ($iteration -gt 1) {
            $testArgs += $reloadTestDataOverride
        }

        $testRun = Invoke-AcceptanceTestsWithMemoryMonitor -DotNetTestArguments $testArgs -ServerProcessId $serverProcess.Id
        $testsPassed = ($testRun.ExitCode -eq 0)
        if (-not $testsPassed -and -not $ContinueOnTestFailure) {
            throw "Iteration $iteration acceptance tests failed with exit code $($testRun.ExitCode)"
        }

        if (-not $testsPassed) {
            Write-Warning "Iteration $iteration acceptance tests failed with exit code $($testRun.ExitCode)"
        }

        Write-Host ("Iteration {0}/{1} memory (MB): before={2}, after={3}, peak={4}, samples={5}" -f `
                $iteration, `
                $Iterations, `
                (Format-MemoryValue $testRun.BeforePrivateMB), `
                (Format-MemoryValue $testRun.AfterPrivateMB), `
                (Format-MemoryValue $testRun.PeakPrivateMB), `
                $testRun.Samples)

        if ($null -eq $baselinePeakPrivateMB) {
            $baselinePeakPrivateMB = $testRun.PeakPrivateMB
        }

        if ($null -ne $previousPeakPrivateMB) {
            $growthFromPrevious = [math]::Round($testRun.PeakPrivateMB - $previousPeakPrivateMB, 2)
            if ($growthFromPrevious -ge $LeakGrowthThresholdMB) {
                $consecutiveGrowth++
            }
            elseif ($growthFromPrevious -lt 0) {
                $consecutiveGrowth = 0
            }
        }

        $growthFromBaseline = [math]::Round($testRun.PeakPrivateMB - $baselinePeakPrivateMB, 2)
        if ($consecutiveGrowth -ge $ConsecutiveGrowthCount -or $growthFromBaseline -ge ($LeakGrowthThresholdMB * $ConsecutiveGrowthCount)) {
            $leakDetected = $true
            Write-Warning "Potential memory leak signal after iteration $iteration. PeakPrivateMB=$($testRun.PeakPrivateMB) GrowthFromBaselineMB=$growthFromBaseline ConsecutiveGrowthCount=$consecutiveGrowth"
        }

        $leftoverServerCount = (Get-UiServerProcesses | Measure-Object).Count
        if ($leftoverServerCount -gt 1) {
            Write-Warning "Detected $leftoverServerCount UI.Server process(es) after iteration $iteration."
        }

        $results.Add([PSCustomObject]@{
            Iteration = $iteration
            TestsPassed = $testsPassed
            ServerDetected = $testRun.ServerDetected
            BeforePrivateMB = $testRun.BeforePrivateMB
            AfterPrivateMB = $testRun.AfterPrivateMB
            PeakPrivateMB = $testRun.PeakPrivateMB
            PeakWorkingSetMB = $testRun.PeakWorkingSetMB
            Samples = $testRun.Samples
            LeftoverServerCount = $leftoverServerCount
            GrowthFromBaselineMB = $growthFromBaseline
            ConsecutiveGrowthCount = $consecutiveGrowth
        })

        $previousPeakPrivateMB = $testRun.PeakPrivateMB
    }

    $results | Format-Table -AutoSize
    if ($leakDetected) {
        Write-Warning "Leak signals detected in churn run."
    }
    else {
        Write-Host "No leak signals detected using configured thresholds."
    }
}
finally {
    [Environment]::SetEnvironmentVariable("StartLocalServer", $originalStartLocalServer, "Process")
    [Environment]::SetEnvironmentVariable("ApplicationBaseUrl", $originalApplicationBaseUrl, "Process")
    if ($serverProcess) {
        $runningServer = Get-Process -Id $serverProcess.Id -ErrorAction SilentlyContinue
        if ($runningServer) {
            Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
            $runningServer = Get-Process -Id $serverProcess.Id -ErrorAction SilentlyContinue
            if ($runningServer) {
                throw "Failed to stop UI.Server process (PID $($serverProcess.Id))."
            }
        }
    }
}
