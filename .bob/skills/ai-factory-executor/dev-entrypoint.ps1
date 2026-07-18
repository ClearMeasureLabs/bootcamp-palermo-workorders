#!/usr/bin/env pwsh
<#
.SYNOPSIS
    AI Factory interactive dev-workstation entrypoint.
.DESCRIPTION
    Prepares the container as a ready-to-use development environment and then
    idles (no work item dispatched):
      1. Starts Docker-in-Docker (for the SQL Server container).
      2. Configures gh/git + agent credentials.
      3. Runs the full PrivateBuild (dot-sourced so the SQL connection string it
         computes can be captured) - this compiles, sets up the SQL Server
         database via DinD, migrates it, and runs the test suites.
      4. Persists the DB connection string to /home/bobagent/.dev-connection so
         serve-app.ps1 can start the app against the same database.
      5. Prints a READY banner and idles, so a user can `docker exec -it` in and
         run claude / bob / dotnet interactively.
    Expects the host repository bind-mounted at /workspace.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
function Write-Info { param([string]$m) Write-Host "[INFO] $m" -ForegroundColor Yellow }
function Write-Ok   { param([string]$m) Write-Host "[OK] $m" -ForegroundColor Green }
function Write-Step { param([string]$m) Write-Host "[->] $m" -ForegroundColor Cyan }

try {
    $repoOrg  = if ($env:REPO_ORG) { $env:REPO_ORG } else { "ClearMeasureLabs" }
    $repoName = if ($env:REPO_NAME) { $env:REPO_NAME } else { "bootcamp-palermo-workorders" }

    Write-Step "AI Factory dev workstation starting..."

    # NuGet cache volume perms (mounted at /tmp/nuget-packages to match build.ps1)
    if (Test-Path "/tmp/nuget-packages") {
        sudo chown -R bobagent:bobagent /tmp/nuget-packages 2>$null
    }

    # 1. Docker-in-Docker (required for SQL Server container mode)
    $env:ENABLE_DIND = "true"
    & /usr/local/bin/start-dind.ps1

    # 2. Credentials: gh + git, Claude, Bob
    if (Test-Path "/run/secrets/gh_token") {
        Get-Content "/run/secrets/gh_token" | gh auth login --with-token
        $ghToken = Get-Content "/run/secrets/gh_token" -Raw
        git config --global user.name "AI Factory Bot"
        git config --global user.email "ai-factory-bot@users.noreply.github.com"
        git config --global credential.helper store
        Set-Content -Path "/home/bobagent/.git-credentials" -Value "https://x-access-token:$ghToken@github.com" -NoNewline
        Write-Ok "GitHub configured"
    }
    if (-not $env:ANTHROPIC_API_KEY -and (Test-Path "/run/secrets/anthropic_api_key")) {
        $env:ANTHROPIC_API_KEY = (Get-Content "/run/secrets/anthropic_api_key" -Raw).Trim()
    }
    if (Test-Path "/run/secrets/claude_credentials") {
        New-Item -ItemType Directory -Force -Path "/home/bobagent/.claude" | Out-Null
        Copy-Item "/run/secrets/claude_credentials" "/home/bobagent/.claude/.credentials.json" -Force
        Write-Ok "Claude credentials installed"
    }
    if (-not $env:BOBSHELL_API_KEY -and (Test-Path "/run/secrets/bobshell_api_key")) {
        $env:BOBSHELL_API_KEY = (Get-Content "/run/secrets/bobshell_api_key" -Raw).Trim()
        $env:GEMINI_API_KEY = $env:BOBSHELL_API_KEY
    }

    # 3. Prepare the bind-mounted workspace
    if (-not (Test-Path "/workspace/build.ps1")) {
        throw "No repository found at /workspace. Bind-mount the host repo to /workspace (run-dev.ps1 does this)."
    }
    Set-Location /workspace
    git config --global --add safe.directory /workspace 2>$null
    Write-Ok "Workspace ready at /workspace ($(git rev-parse --abbrev-ref HEAD 2>$null))"

    # 4. Run the full build (dot-sourced to capture the SQL connection string).
    Write-Step "Running PrivateBuild (SQL Server via Docker-in-Docker) - compiling, DB setup, migrations, tests..."
    . ./build.ps1
    Build
    $conn = $env:ConnectionStrings__SqlConnectionString
    if ($conn) {
        Set-Content -Path "/home/bobagent/.dev-connection" -Value $conn -NoNewline
        Write-Ok "Database ready; connection string captured for serve-app"
    } else {
        Write-Info "No SQL connection string captured (SQLite fallback?) - serve-app will use defaults"
    }

    # 5. READY banner + idle
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host " DEV WORKSTATION READY - container idle, waiting for you" -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host " Terminal in:   docker exec -it $env:HOSTNAME pwsh   (or use enter-dev.ps1)" -ForegroundColor White
    Write-Host " Start the app: pwsh /usr/local/bin/serve-app.ps1   (then browse the mapped HTTPS port)" -ForegroundColor White
    Write-Host " Run an agent:  claude   |   bob --accept-license" -ForegroundColor White
    Write-Host " Workspace:     /workspace (bind-mounted host repo)" -ForegroundColor White
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host ""

    while ($true) { Start-Sleep -Seconds 3600 }

} catch {
    Write-Host "[ERROR] Dev workstation setup failed: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host "[INFO] Container kept alive for inspection (docker exec -it <name> pwsh). Idling." -ForegroundColor Yellow
    while ($true) { Start-Sleep -Seconds 3600 }
}
