#!/usr/bin/env pwsh
<#
.SYNOPSIS
    AI Factory Agent Container Entrypoint
.DESCRIPTION
    Autonomous agent that implements a GitHub issue from clone to green PR.
    Runs inside a Docker container with full credentials and tooling.
    Executes PrivateBuild.ps1 to ensure quality before committing.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Color output helpers (ASCII only - avoids PowerShell UTF-8 parsing issues in containers)
function Write-Success { param([string]$Message) Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }
function Write-Progress { param([string]$Message) Write-Host "[->] $Message" -ForegroundColor Cyan }
function Write-Info { param([string]$Message) Write-Host "[INFO] $Message" -ForegroundColor Yellow }

# Structured logging for main agent parsing
function Write-StructuredLog {
    param(
        [string]$Level,
        [string]$Message,
        [hashtable]$Data = @{}
    )
    
    $logEntry = @{
        timestamp = (Get-Date).ToUniversalTime().ToString("o")
        level = $Level
        message = $Message
    } + $Data
    
    Write-Host ($logEntry | ConvertTo-Json -Compress)
}

try {
    Write-StructuredLog -Level "INFO" -Message "AI Factory Agent starting..."

    # Ensure the (possibly root-owned) persistent NuGet cache volume is writable
    # by bobagent. Mounted at /tmp/nuget-packages to match build.ps1, which sets
    # NUGET_PACKAGES=/tmp/nuget-packages. Safe no-op when no volume is mounted.
    if (Test-Path "/tmp/nuget-packages") {
        sudo chown -R bobagent:bobagent /tmp/nuget-packages 2>$null
    }

    # Start Docker-in-Docker if requested (no-op unless ENABLE_DIND=true).
    & /usr/local/bin/start-dind.ps1
    
    # 1. Validate required environment variables
    $requiredVars = @(
        "ISSUE_NUMBER",
        "ISSUE_TITLE",
        "ISSUE_BODY",
        "ISSUE_URL",
        "BRANCH_NAME",
        "REPO_ORG",
        "REPO_NAME"
    )
    
    foreach ($var in $requiredVars) {
        if (-not (Test-Path "env:$var")) {
            throw "Required environment variable $var is not set"
        }
    }
    
    $issueNumber = $env:ISSUE_NUMBER
    $issueTitle = $env:ISSUE_TITLE
    $issueBody = $env:ISSUE_BODY
    $issueUrl = $env:ISSUE_URL
    $branchName = $env:BRANCH_NAME
    $repoOrg = $env:REPO_ORG
    $repoName = $env:REPO_NAME
    
    Write-StructuredLog -Level "INFO" -Message "Processing issue" -Data @{
        issue_number = $issueNumber
        issue_title = $issueTitle
        branch_name = $branchName
    }
    
    # 2. Configure GitHub CLI authentication
    Write-Progress "Configuring GitHub CLI authentication..."
    
    if (Test-Path "/run/secrets/gh_token") {
        Get-Content "/run/secrets/gh_token" | gh auth login --with-token
        Write-Success "GitHub CLI authenticated"
    } else {
        throw "GitHub token secret not found at /run/secrets/gh_token"
    }
    
    # 3. Configure git with GitHub token authentication
    Write-Progress "Configuring git..."
    git config --global user.name "AI Factory Bot"
    git config --global user.email "ai-factory-bot@users.noreply.github.com"
    git config --global init.defaultBranch master
    
    # Configure git to use GitHub token for HTTPS authentication
    $ghToken = Get-Content "/run/secrets/gh_token" -Raw
    git config --global credential.helper store
    $credentialContent = "https://x-access-token:$ghToken@github.com"
    Set-Content -Path "/home/bobagent/.git-credentials" -Value $credentialContent -NoNewline
    
    Write-Success "Git configured with GitHub authentication"
    
    # 4. Clone repository
    Write-Progress "Cloning repository $repoOrg/$repoName..."
    Write-StructuredLog -Level "INFO" -Message "Cloning repository" -Data @{
        repo = "$repoOrg/$repoName"
    }
    
    $repoUrl = "https://github.com/$repoOrg/$repoName.git"
    git clone --depth 1 $repoUrl /workspace 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to clone repository"
    }
    
    Set-Location /workspace
    Write-Success "Repository cloned"
    
    # 5. Create feature branch
    Write-Progress "Creating branch: $branchName"
    Write-StructuredLog -Level "INFO" -Message "Creating branch" -Data @{
        branch_name = $branchName
    }
    
    git checkout -b $branchName 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create branch $branchName"
    }
    
    Write-Success "Branch created: $branchName"

    # Force normal build verbosity so PrivateBuild/AcceptanceTests stream full
    # console output to docker logs. build.ps1 hardcodes `$verbosity = "quiet"`;
    # the container clones master, so patch the working copy at runtime. (The
    # repo file is also updated to "normal" for when this reaches master.)
    if (Test-Path "./build.ps1") {
        (Get-Content "./build.ps1") -replace '\$verbosity = "quiet"', '$verbosity = "normal"' |
            Set-Content "./build.ps1"
        Write-Info "Build verbosity set to normal"
    }

    # 6. Build the task prompt for Bob CLI
    $task = @"
Implement GitHub issue #${issueNumber}: $issueTitle

**Issue URL:** $issueUrl

**Description:**
$issueBody

**Instructions:**
1. Analyze the issue requirements carefully
2. Implement the changes following the issue's acceptance criteria
3. Follow existing code patterns and architecture (see CLAUDE.md and AGENTS.md)
4. Run PrivateBuild.ps1 to ensure all tests pass
5. Commit with message: "feat: $issueTitle (#$issueNumber)"

**Quality Checks (via PrivateBuild.ps1):**
- Clean and restore dependencies
- Compile solution
- Run unit tests
- Setup database (SQLite mode in container)
- Run integration tests
- All checks must pass before committing

**Constraints:**
- Follow the project's coding standards
- Use SQLite for database (DATABASE_ENGINE=SQLite)
- Do not merge - only implement and push
- If blocked or requirements unclear, document the blocker
"@

    Write-StructuredLog -Level "INFO" -Message "Task prepared for implementation"
    
    # 7. Execute implementation
    Write-Progress "Executing implementation..."
    Write-StructuredLog -Level "INFO" -Message "Starting implementation"
    
    # Database engine: with Docker-in-Docker available, let build.ps1 auto-detect
    # and use its SQL-Server container mode. Otherwise force SQLite (no daemon).
    if ($env:ENABLE_DIND -eq "true") {
        Write-Info "Docker-in-Docker enabled - build will auto-detect SQL Server container mode"
    } else {
        $env:DATABASE_ENGINE = "SQLite"
        $env:ConnectionStrings__SqlConnectionString = "Data Source=ChurchBulletin.db"
    }

    # Select which AI agent to run. Default: claude. Swap to bob by setting AI_AGENT=bob.
    $aiAgent = if ($env:AI_AGENT) { $env:AI_AGENT.ToLower() } else { "claude" }
    Write-Info "AI agent: $aiAgent"
    Write-StructuredLog -Level "INFO" -Message "Selected AI agent" -Data @{ agent = $aiAgent }

    # Build the implementation prompt (shared by all agents).
    # The agent only edits code; the surrounding script handles branch/commit/push/PR.
    $agentPrompt = @"
Implement GitHub issue #${issueNumber}: $issueTitle

Description:
$issueBody

Instructions:
- Analyze the requirements and implement the change in this repository.
- Follow existing code patterns and architecture (read CLAUDE.md and AGENTS.md if present).
- Make only the code/file changes needed to satisfy the issue. Keep the change minimal and focused.
- Do NOT run git (no commit, push, branch, or PR). Do NOT run builds or long test suites.
- When finished, briefly summarize the files you changed.
"@

    switch ($aiAgent) {
        "bob" {
            # Bob Shell headless auth requires BOBSHELL_API_KEY
            if (-not $env:BOBSHELL_API_KEY -and (Test-Path "/run/secrets/bobshell_api_key")) {
                $env:BOBSHELL_API_KEY = (Get-Content "/run/secrets/bobshell_api_key" -Raw).Trim()
            }
            if (-not $env:BOBSHELL_API_KEY) {
                throw "BOBSHELL_API_KEY is not set. Bob Shell cannot authenticate without it."
            }
            $env:GEMINI_API_KEY = $env:BOBSHELL_API_KEY
            New-Item -ItemType Directory -Force -Path "/home/bobagent/.bob" | Out-Null

            Write-Progress "Running Bob Shell agent to implement the issue..."
            Write-StructuredLog -Level "INFO" -Message "Invoking Bob Shell agent"

            $agentPrompt | bob `
                --accept-license `
                --approval-mode yolo `
                --trust `
                --hide-intermediary-output 2>&1 | Write-Host

            if ($LASTEXITCODE -ne 0) {
                throw "Bob Shell agent failed with exit code $LASTEXITCODE"
            }
        }
        "claude" {
            # Claude Code headless auth: mounted OAuth credentials (or ANTHROPIC_API_KEY)
            if (-not $env:ANTHROPIC_API_KEY -and (Test-Path "/run/secrets/anthropic_api_key")) {
                $env:ANTHROPIC_API_KEY = (Get-Content "/run/secrets/anthropic_api_key" -Raw).Trim()
            }
            if (Test-Path "/run/secrets/claude_credentials") {
                New-Item -ItemType Directory -Force -Path "/home/bobagent/.claude" | Out-Null
                Copy-Item "/run/secrets/claude_credentials" "/home/bobagent/.claude/.credentials.json" -Force
                Write-Info "Claude OAuth credentials installed"
            }
            if (-not $env:ANTHROPIC_API_KEY -and -not (Test-Path "/home/bobagent/.claude/.credentials.json")) {
                throw "No Claude credentials found (need ANTHROPIC_API_KEY or mounted OAuth credentials)."
            }

            Write-Progress "Running Claude Code agent (Remote Control) to implement the issue..."
            Write-StructuredLog -Level "INFO" -Message "Invoking Claude Code agent"

            # Run the agent as a Remote Control session so it is visible and
            # controllable in the Claude mobile app. Remote Control sessions are
            # interactive and do NOT exit at stdin EOF, so we cannot block on them
            # like print mode. Instead the agent creates a sentinel file when done
            # and we poll for it, then stop the session and continue the pipeline.
            $rcName = "ai-factory-issue-$issueNumber"
            $sentinel = "/tmp/agent-complete"          # in /tmp so it never touches the git tree
            $promptFile = "/tmp/issue-prompt.txt"
            $agentLog = "/tmp/agent-output.log"
            Remove-Item $sentinel, $agentLog -Force -ErrorAction SilentlyContinue

            $claudePrompt = $agentPrompt + @"


IMPORTANT - completion signal: after you have finished ALL changes for this
issue, run this exact command as your final action to signal completion:
  touch $sentinel
Do this only once, after the implementation is complete.
"@
            Set-Content -Path $promptFile -Value $claudePrompt -NoNewline

            # Launch claude DIRECTLY in a detached tmux session so its stdout is
            # the tmux pty. Remote Control needs an interactive pty; piping the
            # output (e.g. | tee) makes stdout a pipe and RC never registers.
            # Capture output for docker logs via `tmux pipe-pane` instead.
            $claudeCmd = "cd /workspace && claude --remote-control `"$rcName`" --dangerously-skip-permissions `"`$(cat $promptFile)`""
            tmux kill-session -t agent 2>$null
            tmux new-session -d -s agent -x 220 -y 50 $claudeCmd
            tmux pipe-pane -o -t agent "cat >> $agentLog"
            Write-Success "Claude Remote Control session '$rcName' started"
            Write-StructuredLog -Level "INFO" -Message "Remote Control session started" -Data @{ session = $rcName }

            # Give RC a few seconds to register, then surface the session link so
            # it can be opened/verified in the Claude mobile app.
            Start-Sleep -Seconds 8
            $paneNow = (tmux capture-pane -t agent -p -S -80 2>$null) -join "`n"
            $rcUrl = [regex]::Match($paneNow, 'https://claude\.ai/code/session_\w+').Value
            if ($rcUrl) {
                Write-Success "Remote Control ACTIVE - open in the Claude app: $rcUrl"
                Write-StructuredLog -Level "INFO" -Message "Remote Control session link" -Data @{ url = $rcUrl }
            } else {
                Write-Info "Remote Control link not detected yet; session '$rcName' should appear in the Claude app."
            }

            # Poll for the completion sentinel (or session death / timeout).
            $agentTimeoutMin = 25
            $deadline = (Get-Date).AddMinutes($agentTimeoutMin)
            while (-not (Test-Path $sentinel)) {
                tmux has-session -t agent 2>$null
                if ($LASTEXITCODE -ne 0) {
                    Write-Info "Agent tmux session ended before signaling completion"
                    break
                }
                if ((Get-Date) -gt $deadline) {
                    tmux kill-session -t agent 2>$null
                    throw "Claude agent did not signal completion within $agentTimeoutMin minutes"
                }
                Start-Sleep -Seconds 5
            }

            # Surface the agent transcript into container logs. Leave the Remote
            # Control session ALIVE so it stays visible/controllable in the app
            # while the build/PR pipeline runs; it is torn down when the container
            # exits. Changes are snapshotted (committed) from the current tree.
            if (Test-Path $agentLog) {
                Write-Info "--- Claude agent output (tail) ---"
                Get-Content $agentLog -Tail 40 | ForEach-Object { Write-Host $_ }
                Write-Info "--- end agent output ---"
            }
            Write-Info "Remote Control session '$rcName' left running for the app; continuing pipeline."
            Remove-Item $promptFile -Force -ErrorAction SilentlyContinue
        }
        default {
            throw "Unknown AI_AGENT '$aiAgent'. Supported values: claude, bob."
        }
    }

    # Verify the agent actually changed something. Exclude build.ps1, which the
    # entrypoint patched at runtime for verbose logging - that is not the agent's
    # work and is reverted before committing.
    $changes = git status --porcelain | Where-Object { $_ -notmatch 'build\.ps1' }
    if (-not $changes) {
        throw "AI agent ($aiAgent) produced no file changes for issue #$issueNumber"
    }

    Write-Success "AI agent ($aiAgent) implemented the issue"
    Write-StructuredLog -Level "INFO" -Message "Agent implementation complete" -Data @{
        agent = $aiAgent
        changed_files = (($changes -split "`n") | Measure-Object).Count
    }

    # 8b. Quality gates - run PrivateBuild and AcceptanceTests BEFORE pushing.
    #     Set RUN_QUALITY_GATES=false to skip (e.g. for fast smoke tests).
    if ($env:RUN_QUALITY_GATES -eq "false") {
        Write-Info "RUN_QUALITY_GATES=false - skipping PrivateBuild and AcceptanceTests"
        Write-StructuredLog -Level "INFO" -Message "Skipping quality gates"
    } else {
        # PrivateBuild.ps1: compile + unit tests + DB migration + integration tests (SQLite mode)
        # Output streams live to `docker logs` because the container is started
        # with a TTY (-t), which makes stdout line-buffered end-to-end.
        Write-Progress "Quality gate 1/2: Running PrivateBuild.ps1..."
        Write-StructuredLog -Level "INFO" -Message "Running PrivateBuild"
        & pwsh -File ./PrivateBuild.ps1
        if ($LASTEXITCODE -ne 0) {
            Write-StructuredLog -Level "ERROR" -Message "PrivateBuild failed" -Data @{ exit_code = $LASTEXITCODE }
            throw "PrivateBuild.ps1 failed with exit code $LASTEXITCODE - not pushing."
        }
        Write-Success "PrivateBuild passed"

        # AcceptanceTests.ps1: Playwright acceptance tests (SQLite mode)
        Write-Progress "Quality gate 2/2: Running AcceptanceTests.ps1..."
        Write-StructuredLog -Level "INFO" -Message "Running AcceptanceTests"
        & pwsh -File ./AcceptanceTests.ps1
        if ($LASTEXITCODE -ne 0) {
            Write-StructuredLog -Level "ERROR" -Message "AcceptanceTests failed" -Data @{ exit_code = $LASTEXITCODE }
            throw "AcceptanceTests.ps1 failed with exit code $LASTEXITCODE - not pushing."
        }
        Write-Success "AcceptanceTests passed"
        Write-StructuredLog -Level "INFO" -Message "All quality gates passed"
    }

    # 9. Commit changes
    Write-Progress "Committing changes..."

    # Revert the runtime verbosity patch so build.ps1 is not part of the PR.
    # (No-op once this change reaches master, where build.ps1 is already normal.)
    git checkout -- build.ps1 2>$null

    git add -A
    git commit -m "feat: $issueTitle (#$issueNumber)" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Changes committed"
        Write-StructuredLog -Level "INFO" -Message "Changes committed"
    } else {
        # Check if there are no changes to commit
        $status = git status --porcelain
        if (-not $status) {
            Write-Info "No changes to commit, creating empty commit"
            git commit --allow-empty -m "feat: $issueTitle (#$issueNumber)" 2>&1 | Out-Null
        } else {
            throw "Failed to commit changes"
        }
    }
    
    # 10. Push branch
    Write-Progress "Pushing branch to remote..."
    Write-StructuredLog -Level "INFO" -Message "Pushing branch"
    
    git push origin $branchName 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to push branch"
    }
    
    Write-Success "Branch pushed"
    
    # 11. Create PR
    Write-Progress "Creating pull request..."
    Write-StructuredLog -Level "INFO" -Message "Creating pull request"
    
    $prBody = "Closes #$issueNumber`n`n$issueBody"

    # If a PR already exists for this branch (re-run), reuse it instead of failing.
    $existingPr = gh pr list --repo "$repoOrg/$repoName" --head $branchName --state open --json number 2>&1
    $prNumber = $null
    if ($LASTEXITCODE -eq 0 -and $existingPr -and ($existingPr | ConvertFrom-Json).Count -gt 0) {
        $prNumber = ($existingPr | ConvertFrom-Json)[0].number
        Write-Info "PR already exists for branch $branchName - reusing #$prNumber"
    } else {
        $prOutput = gh pr create `
            --repo "$repoOrg/$repoName" `
            --title "$issueTitle" `
            --body "$prBody" `
            --head $branchName `
            --base master 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create PR: $prOutput"
        }

        # Extract PR number from output. `gh pr create` prints the PR URL
        # (e.g. https://github.com/org/repo/pull/6972); fall back to #123 form.
        $prOutputText = ($prOutput | Out-String)
        if ($prOutputText -match '/pull/(\d+)') {
            $prNumber = $matches[1]
        } elseif ($prOutputText -match '#(\d+)') {
            $prNumber = $matches[1]
        }
    }
    
    Write-Success "PR created: #$prNumber"
    Write-StructuredLog -Level "INFO" -Message "Pull request created" -Data @{
        pr_number = $prNumber
    }

    # 11b. KEEP_ALIVE serve mode - run the app with the issue implemented and
    #      expose it via a Cloudflare quick tunnel so it can be viewed live.
    #      Enabled with KEEP_ALIVE=true. Runs after the PR so the served build is
    #      the exact code that passed the quality gates.
    if ($env:KEEP_ALIVE -eq "true") {
        Write-Progress "KEEP_ALIVE=true - starting app + Cloudflare tunnel..."
        Write-StructuredLog -Level "INFO" -Message "Entering serve mode" -Data @{ issue_number = $issueNumber; pr_number = $prNumber }

        $servePort = if ($env:SERVE_PORT) { $env:SERVE_PORT } else { "8080" }
        $env:ASPNETCORE_URLS = "http://0.0.0.0:$servePort"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        # DATABASE_ENGINE/SQLite connection string were set earlier (non-DIND);
        # PrivateBuild already created the SQLite DB in /workspace.

        # Launch the server detached so we can tunnel and keep the container alive.
        $appLog = "/tmp/app.log"
        tmux kill-session -t app 2>$null
        tmux new-session -d -s app "cd /workspace && dotnet run --project src/UI/Server -c Release *>> $appLog"
        Write-Info "App starting on :$servePort (logs -> $appLog)"

        # Wait for the app to answer its health check (up to 5 min for first JIT).
        $healthy = $false
        for ($i = 0; $i -lt 60; $i++) {
            Start-Sleep -Seconds 5
            try {
                $r = Invoke-WebRequest -UseBasicParsing "http://localhost:$servePort/_healthcheck" -TimeoutSec 4
                if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { $healthy = $true; break }
            } catch { }
        }
        if ($healthy) {
            Write-Success "App is serving on :$servePort"
            Write-StructuredLog -Level "INFO" -Message "App healthy" -Data @{ issue_number = $issueNumber; port = $servePort }
        } else {
            Write-Info "App health check not confirmed; starting tunnel anyway"
        }

        # Start a Cloudflare quick tunnel and capture the public trycloudflare URL.
        $tunnelLog = "/tmp/cloudflared.log"
        tmux kill-session -t tunnel 2>$null
        tmux new-session -d -s tunnel "cloudflared tunnel --no-autoupdate --url http://localhost:$servePort > $tunnelLog 2>&1"
        $publicUrl = $null
        for ($i = 0; $i -lt 45; $i++) {
            Start-Sleep -Seconds 2
            if (Test-Path $tunnelLog) {
                $m = [regex]::Match((Get-Content $tunnelLog -Raw), 'https://[a-z0-9-]+\.trycloudflare\.com')
                if ($m.Success) { $publicUrl = $m.Value; break }
            }
        }
        if ($publicUrl) {
            Write-Success "LIVE APP URL: $publicUrl  (issue #$issueNumber implemented, PR #$prNumber)"
            # Distinctive, easily-greppable marker for orchestrators/tests.
            Write-Host "AI_FACTORY_LIVE_URL issue=$issueNumber pr=$prNumber url=$publicUrl"
            Write-StructuredLog -Level "SUCCESS" -Message "App live via Cloudflare tunnel" -Data @{
                issue_number = $issueNumber; pr_number = $prNumber; public_url = $publicUrl; port = $servePort
            }
        } else {
            Write-Failure "Could not obtain Cloudflare tunnel URL (see $tunnelLog)"
            Write-StructuredLog -Level "ERROR" -Message "Tunnel URL not obtained" -Data @{ issue_number = $issueNumber }
        }

        # Hold the container open so the app + tunnel stay reachable for inspection.
        Write-Info "Container held open for inspection (KEEP_ALIVE). Stop it to tear down the app + tunnel."
        while ($true) { Start-Sleep -Seconds 3600 }
    }

    # 12. Monitor PR checks until all green (optional; set MONITOR_CHECKS=false to skip)
    if ($env:MONITOR_CHECKS -eq "false") {
        Write-Info "MONITOR_CHECKS=false - skipping PR check monitoring (checks run async on GitHub)"
        Write-StructuredLog -Level "INFO" -Message "Skipping PR check monitoring" -Data @{ pr_number = $prNumber }
        Write-Success "Issue #$issueNumber implemented successfully (PR #$prNumber)"
        Write-StructuredLog -Level "SUCCESS" -Message "Issue implementation complete" -Data @{
            issue_number = $issueNumber
            pr_number = $prNumber
            branch_name = $branchName
        }
        exit 0
    }

    Write-Progress "Monitoring PR checks..."
    Write-StructuredLog -Level "INFO" -Message "Starting PR check monitoring"

    $maxWaitMinutes = 30
    $startTime = Get-Date
    $checkInterval = 30 # seconds

    while ($true) {
        # Check timeout
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalMinutes -gt $maxWaitMinutes) {
            Write-Failure "Timeout waiting for PR checks (${maxWaitMinutes}m)"
            Write-StructuredLog -Level "WARNING" -Message "PR check monitoring timeout" -Data @{
                pr_number = $prNumber
                elapsed_minutes = [int]$elapsed.TotalMinutes
            }
            break
        }
        
        # Get PR check status
        $checksJson = gh pr checks $prNumber --json state,name 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Info "Unable to query PR checks, continuing..."
            Start-Sleep -Seconds $checkInterval
            continue
        }
        
        $checks = $checksJson | ConvertFrom-Json
        
        if (-not $checks -or $checks.Count -eq 0) {
            Write-Info "No checks found yet, waiting..."
            Start-Sleep -Seconds $checkInterval
            continue
        }
        
        # Count check states
        $pending = @($checks | Where-Object { $_.state -in @("PENDING", "QUEUED", "IN_PROGRESS") })
        $failed = @($checks | Where-Object { $_.state -in @("FAILURE", "CANCELLED", "TIMED_OUT") })
        $success = @($checks | Where-Object { $_.state -eq "SUCCESS" })
        
        Write-Info "PR checks: $($success.Count) passed, $($pending.Count) pending, $($failed.Count) failed"
        
        # If any checks failed, report and exit
        if ($failed.Count -gt 0) {
            Write-Failure "PR checks failed:"
            foreach ($check in $failed) {
                Write-Host "  - $($check.name): $($check.state)" -ForegroundColor Red
            }
            Write-StructuredLog -Level "ERROR" -Message "PR checks failed" -Data @{
                pr_number = $prNumber
                failed_checks = ($failed | ForEach-Object { $_.name })
            }
            throw "PR checks failed"
        }
        
        # If all checks passed, we're done
        if ($pending.Count -eq 0 -and $success.Count -gt 0) {
            Write-Success "All PR checks passed!"
            Write-StructuredLog -Level "SUCCESS" -Message "All PR checks passed" -Data @{
                pr_number = $prNumber
                issue_number = $issueNumber
                total_checks = $success.Count
            }
            break
        }
        
        # Wait before next check
        Start-Sleep -Seconds $checkInterval
    }
    
    # 13. Final success report
    Write-Success "Issue #$issueNumber implemented successfully"
    Write-StructuredLog -Level "SUCCESS" -Message "Issue implementation complete" -Data @{
        issue_number = $issueNumber
        pr_number = $prNumber
        branch_name = $branchName
    }
    
    exit 0
    
} catch {
    Write-Failure "Agent execution failed: $_"
    Write-StructuredLog -Level "ERROR" -Message "Agent execution failed" -Data @{
        error = $_.Exception.Message
        issue_number = $env:ISSUE_NUMBER
    }
    
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}