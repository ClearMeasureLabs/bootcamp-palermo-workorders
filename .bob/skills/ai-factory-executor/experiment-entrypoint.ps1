#!/usr/bin/env pwsh
<#
.SYNOPSIS
    AI Factory experiment entrypoint - launches an agent in an attachable tmux
    session for remote inspection / control, and keeps the container alive.
.DESCRIPTION
    Clones the repo, authenticates the agent, and starts the selected AI agent
    (claude or bob) inside a tmux session named "agent" seeded with an initial
    prompt. The container stays up for EXPERIMENT_TIMEOUT_MINUTES so an operator
    can attach from the host:

        docker exec -it <container> tmux attach -t agent     # live control
        docker exec -it <container> bash                     # inspect workspace

    Detach from tmux without stopping the agent with:  Ctrl-b then d
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Write-Info { param([string]$m) Write-Host "[INFO] $m" -ForegroundColor Yellow }
function Write-Ok   { param([string]$m) Write-Host "[OK] $m" -ForegroundColor Green }
function Write-Step { param([string]$m) Write-Host "[->] $m" -ForegroundColor Cyan }

try {
    $aiAgent    = if ($env:AI_AGENT) { $env:AI_AGENT.ToLower() } else { "claude" }
    $initPrompt = if ($env:INIT_PROMPT) { $env:INIT_PROMPT } else { "Explore this repository and summarize its architecture. Wait for further instructions." }
    $repoOrg    = if ($env:REPO_ORG) { $env:REPO_ORG } else { "ClearMeasureLabs" }
    $repoName   = if ($env:REPO_NAME) { $env:REPO_NAME } else { "bootcamp-palermo-workorders" }
    $timeoutMin = if ($env:EXPERIMENT_TIMEOUT_MINUTES) { [int]$env:EXPERIMENT_TIMEOUT_MINUTES } else { 60 }

    Write-Info "AI agent: $aiAgent"
    Write-Info "Timeout: $timeoutMin min"

    # NuGet cache volume perms (if mounted)
    if (Test-Path "/home/bobagent/.nuget/packages") {
        sudo chown -R bobagent:bobagent /home/bobagent/.nuget/packages 2>$null
    }

    # 1. GitHub auth
    Write-Step "Configuring GitHub CLI + git..."
    if (Test-Path "/run/secrets/gh_token") {
        Get-Content "/run/secrets/gh_token" | gh auth login --with-token
        $ghToken = Get-Content "/run/secrets/gh_token" -Raw
        git config --global user.name "AI Factory Bot"
        git config --global user.email "ai-factory-bot@users.noreply.github.com"
        git config --global credential.helper store
        Set-Content -Path "/home/bobagent/.git-credentials" -Value "https://x-access-token:$ghToken@github.com" -NoNewline
        Write-Ok "GitHub configured"
    } else {
        Write-Info "No gh token mounted - git remote operations will not be authenticated"
    }

    # 2. Agent auth
    if ($aiAgent -eq "claude") {
        if (-not $env:ANTHROPIC_API_KEY -and (Test-Path "/run/secrets/anthropic_api_key")) {
            $env:ANTHROPIC_API_KEY = (Get-Content "/run/secrets/anthropic_api_key" -Raw).Trim()
        }
        if (Test-Path "/run/secrets/claude_credentials") {
            New-Item -ItemType Directory -Force -Path "/home/bobagent/.claude" | Out-Null
            Copy-Item "/run/secrets/claude_credentials" "/home/bobagent/.claude/.credentials.json" -Force
            Write-Ok "Claude credentials installed"
        }
        # Seed global config so the first-run onboarding flow (theme picker, login
        # method, trust dialog) is skipped and claude starts straight into the
        # authenticated session - required for headless/remote-control launch.
        $claudeVersion = (claude --version 2>&1) -replace '[^0-9\.].*', ''
        $claudeConfig = @{
            hasCompletedOnboarding        = $true
            lastOnboardingVersion         = $claudeVersion
            theme                         = "dark"
            numStartups                   = 5
            remoteControlAtStartup        = $true
            bypassPermissionsModeAccepted = $true
            projects                      = @{
                "/workspace" = @{
                    hasTrustDialogAccepted        = $true
                    hasCompletedProjectOnboarding = $true
                    projectOnboardingSeenCount    = 1
                    allowedTools                  = @()
                }
            }
        } | ConvertTo-Json -Depth 5
        Set-Content -Path "/home/bobagent/.claude.json" -Value $claudeConfig -NoNewline
        Write-Ok "Claude onboarding + trust + bypass pre-seeded (config written)"
    } elseif ($aiAgent -eq "bob") {
        if (-not $env:BOBSHELL_API_KEY -and (Test-Path "/run/secrets/bobshell_api_key")) {
            $env:BOBSHELL_API_KEY = (Get-Content "/run/secrets/bobshell_api_key" -Raw).Trim()
        }
        $env:GEMINI_API_KEY = $env:BOBSHELL_API_KEY
    }

    # 3. Clone repo
    Write-Step "Cloning $repoOrg/$repoName..."
    if (-not (Test-Path "/workspace/.git")) {
        git clone "https://github.com/$repoOrg/$repoName.git" /workspace 2>&1 | Out-Null
    }
    Set-Location /workspace
    Write-Ok "Workspace ready at /workspace"

    # 4. Persist the init prompt to a file (avoids shell quoting issues)
    $promptFile = "/workspace/.init-prompt.txt"
    Set-Content -Path $promptFile -Value $initPrompt -NoNewline

    $rcName = if ($env:REMOTE_CONTROL_NAME) { $env:REMOTE_CONTROL_NAME } else { "ai-factory-$($env:HOSTNAME)" }

    # 5. Build the in-tmux agent command
    if ($aiAgent -eq "claude") {
        # --remote-control registers the interactive session with the user's
        # Claude account (via the mounted OAuth creds, which carry the
        # user:sessions:claude_code scope) so it can be driven from the Claude
        # mobile/iPhone app. Running inside tmux provides the required pty and
        # also allows local inspection via `docker exec ... tmux attach`.
        Write-Info "Remote Control session name: $rcName"
        $agentCmd = "cd /workspace && claude --remote-control `"$rcName`" --dangerously-skip-permissions `"`$(cat /workspace/.init-prompt.txt)`"; echo '[agent exited - shell kept alive for inspection]'; exec bash"
    } else {
        $agentCmd = 'cd /workspace && bob --accept-license --approval-mode yolo --trust -i "$(cat /workspace/.init-prompt.txt)"; echo "[agent exited - shell kept alive for inspection]"; exec bash'
    }

    # 6. Start the agent inside a detached tmux session
    Write-Step "Starting agent in tmux session 'agent'..."
    tmux new-session -d -s agent -x 220 -y 50 $agentCmd
    Start-Sleep -Seconds 2
    $sessions = tmux ls 2>&1
    Write-Ok "tmux session started: $sessions"

    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host " REMOTE CONTROL READY - container will stay up for $timeoutMin min" -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host " Attach (control):  docker exec -it $env:HOSTNAME tmux attach -t agent" -ForegroundColor White
    Write-Host " Inspect (shell):   docker exec -it $env:HOSTNAME bash" -ForegroundColor White
    Write-Host " Detach tmux:       Ctrl-b then d   (leaves agent running)" -ForegroundColor White
    Write-Host " Resume transcript: docker exec -it $env:HOSTNAME claude --resume" -ForegroundColor White
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host ""

    # 7. Keep the container alive until timeout (or the tmux session ends)
    $deadline = (Get-Date).AddMinutes($timeoutMin)
    while ((Get-Date) -lt $deadline) {
        $alive = tmux has-session -t agent 2>&1; $hasSession = ($LASTEXITCODE -eq 0)
        $remaining = [int]($deadline - (Get-Date)).TotalMinutes
        Write-Host "[heartbeat] tmux session alive=$hasSession, $remaining min remaining" -ForegroundColor DarkGray
        Start-Sleep -Seconds 60
    }

    Write-Info "Experiment timeout reached - shutting down"
    tmux kill-server 2>&1 | Out-Null
    exit 0

} catch {
    Write-Host "[ERROR] Experiment entrypoint failed: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    # Keep container alive briefly for inspection even on error
    Start-Sleep -Seconds 300
    exit 1
}
