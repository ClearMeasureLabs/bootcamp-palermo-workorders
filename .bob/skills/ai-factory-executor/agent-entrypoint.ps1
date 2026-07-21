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

# Task 4: annotate this pod with the live tunnel URL via the k8s API, so the URL
# is a queryable output of the (long-running) KEEP_ALIVE workflow:
#   kubectl get pod $POD_NAME -o jsonpath='{.metadata.annotations.ai-factory/live-url}'
# Merge-patch a set of annotations onto this pod via the k8s API using the
# mounted service-account token (RBAC: ai-factory-workflow-podpatch). Non-fatal.
function Set-PodAnnotations {
    param([hashtable]$Annotations)
    $podName = $env:POD_NAME
    $ns      = if ($env:POD_NAMESPACE) { $env:POD_NAMESPACE } else { 'ai-factory' }
    $tokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token"
    if (-not $podName -or -not (Test-Path $tokenPath)) {
        Write-Info "POD_NAME or SA token unavailable; skipping pod annotation (log marker still emitted)."
        return
    }
    try {
        $token = (Get-Content $tokenPath -Raw).Trim()
        $api   = "https://kubernetes.default.svc/api/v1/namespaces/$ns/pods/$podName"
        $patch = @{ metadata = @{ annotations = $Annotations } } | ConvertTo-Json -Depth 5 -Compress
        Invoke-RestMethod -Method Patch -Uri $api -Headers @{ Authorization = "Bearer $token" } `
            -Body $patch -ContentType 'application/merge-patch+json' -SkipCertificateCheck | Out-Null
        Write-Success "Published pod annotations: $($Annotations.Keys -join ', ')"
    } catch {
        Write-Info "Could not annotate pod: $_ (log marker still emitted)."
    }
}

function Publish-LiveUrlAnnotation {
    param([string]$Url, [string]$Pr, [string]$Issue)
    Set-PodAnnotations -Annotations @{
        'ai-factory/live-url'  = $Url
        'ai-factory/pr-number' = "$Pr"
        'ai-factory/issue'     = "$Issue"
    }
    Write-StructuredLog -Level "INFO" -Message "Live URL annotation published" -Data @{ url = $Url }
}

function Publish-TerminalAnnotation {
    param([string]$Url, [string]$Issue)
    # The URL embeds the secret base-path token; no separate credentials.
    Set-PodAnnotations -Annotations @{ 'ai-factory/terminal-url' = $Url }
    Write-StructuredLog -Level "INFO" -Message "Terminal URL annotation published" -Data @{ url = $Url }
}

$script:ServePort = if ($env:SERVE_PORT) { $env:SERVE_PORT } else { "8080" }
$script:TunnelLog = "/tmp/cloudflared.log"
$script:AppLog    = "/tmp/app.log"

# Start the Cloudflare quick tunnel (once) pointing at the serve port and return
# the public URL. Called EARLY, in parallel with the agent, so the URL exists
# before the app is up (cloudflared just 502s at the origin until the app binds).
# Idempotent: re-uses the running tunnel so the URL stays stable.
function Start-Tunnel {
    param([string]$Pr = "pending", [string]$Issue)
    tmux has-session -t tunnel 2>$null
    if ($LASTEXITCODE -ne 0) {
        tmux new-session -d -s tunnel "cloudflared tunnel --no-autoupdate --url http://localhost:$script:ServePort > $script:TunnelLog 2>&1"
        Write-Info "Cloudflare tunnel starting -> :$script:ServePort"
    }
    $publicUrl = $null
    for ($i = 0; $i -lt 60; $i++) {
        $raw = Get-Content $script:TunnelLog -Raw -ErrorAction SilentlyContinue
        if ($raw) {
            $m = [regex]::Match($raw, 'https://[a-z0-9-]+\.trycloudflare\.com')
            if ($m.Success) { $publicUrl = $m.Value; break }
        }
        Start-Sleep -Seconds 2
    }
    if ($publicUrl) {
        Write-Success "LIVE APP URL: $publicUrl  (issue #$Issue, PR #$Pr)"
        Write-Host "AI_FACTORY_LIVE_URL issue=$Issue pr=$Pr url=$publicUrl"
        Write-StructuredLog -Level "SUCCESS" -Message "App live via Cloudflare tunnel" -Data @{
            issue_number = $Issue; pr_number = $Pr; public_url = $publicUrl; port = $script:ServePort
        }
        Publish-LiveUrlAnnotation -Url $publicUrl -Pr $Pr -Issue $Issue
        # Expose the URL to the in-container claude agent session so it can report
        # the live-preview URL to the person driving that session (see the prompt).
        Set-Content -Path "/tmp/live-url.txt" -Value $publicUrl -NoNewline -ErrorAction SilentlyContinue
    } else {
        Write-Failure "Could not obtain Cloudflare tunnel URL (see $script:TunnelLog)"
        Write-StructuredLog -Level "ERROR" -Message "Tunnel URL not obtained" -Data @{ issue_number = $Issue }
    }
    return $publicUrl
}

# Start the app on the serve port using the ALREADY-COMPILED Release output
# (the agent's AcceptanceTests gate runs a full `Compile`, which builds the
# Blazor WASM client's _framework assets). This mirrors the known-good launch
# in AcceptanceTests' ServerFixture: `dotnet run --no-build --configuration
# Release --no-launch-profile --urls=...`. A plain `dotnet run` (with build)
# does NOT emit the WASM _framework, so blazor.webassembly.js 404s. Requires the
# solution to have been compiled first; call this AFTER the agent's gates.
# Reconstruct the in-DinD SQL Server connection string the gates used (build.ps1
# SQL-Container mode). BuildFunctions.ps1 derives the container name + password
# deterministically from the DB name, so we can reach the exact DB whose schema
# the migration function created during the gates. Returns $null if unavailable.
function Get-ServeConnectionString {
    if (-not (Test-Path "/workspace/BuildFunctions.ps1")) { return $null }
    try {
        . /workspace/BuildFunctions.ps1
        # Derive the DB name exactly as build.ps1's acceptance path does, so we
        # reconnect to the same container the gates created (name + password are
        # deterministic from the DB name).
        $dbName    = Get-ResolvedDatabaseName -explicitName "" -baseName "ChurchBulletin" -onLinux (Test-IsLinux) -localBuild (Test-IsLocalBuild)
        $server    = Get-DefaultDatabaseServer -engine "SQL-Container"   # localhost
        $container = Get-ContainerName -DatabaseName $dbName             # <dbname>-mssql
        $pw        = Get-SqlServerPassword -ContainerName $container
        return (New-SqlServerConnectionString -server $server -database $dbName -password $pw)
    } catch {
        Write-Info "Could not reconstruct SQL Server connection string: $_"
        return $null
    }
}

function Start-ServeApp {
    param([string]$Issue)
    # DB: the gates ran build.ps1 SQL-Container mode (in-DinD SQL Server), whose
    # migration function created the schema. Reconnect to that same DB and reload
    # a CLEAN canonical sample dataset for manual testing: the integration +
    # Playwright gates leave the DB mutated, so re-run ZDataLoader.LoadData
    # (Clean() + load). The app then serves against the same SQL Server DB. The
    # provider is chosen by connection-string prefix (server=... -> SQL Server).
    $conn = Get-ServeConnectionString
    if (-not $conn) {
        Write-Failure "No SQL Server connection string - cannot serve with sample data (is DinD/SQL-Container mode active?)."
        return
    }

    Write-Progress "Reloading canonical sample data (ZDataLoader) into the SQL Server DB for manual testing..."
    $loadScript = "/tmp/load-sample-data.ps1"
    Set-Content -Path $loadScript -Value @"
`$env:DATABASE_ENGINE = 'SQL-Container'
`$env:ConnectionStrings__SqlConnectionString = '$conn'
Set-Location /workspace
dotnet test src/IntegrationTests --configuration Release --no-build --no-restore ``
    --filter "FullyQualifiedName~ClearMeasure.Bootcamp.IntegrationTests.ZDataLoader.LoadData"
"@
    & pwsh -NoProfile -File $loadScript *>> $script:AppLog
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Sample data reloaded (ZDataLoader)"
        Write-StructuredLog -Level "INFO" -Message "Serve DB reloaded with sample data" -Data @{ issue_number = $Issue }
    } else {
        Write-Info "ZDataLoader reload exit=$LASTEXITCODE - serving with current DB state (see $script:AppLog)"
        Write-StructuredLog -Level "WARNING" -Message "Serve DB reload failed" -Data @{ issue_number = $Issue; exit_code = $LASTEXITCODE }
    }

    # tmux's default shell is bash, so set env inside a pwsh SCRIPT (inline
    # $env: syntax in a bash command line would be silently dropped -> the app
    # would boot in Production and not serve the WASM static assets).
    $serveScript = "/tmp/serve-app.ps1"
    Set-Content -Path $serveScript -Value @"
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:DATABASE_ENGINE = 'SQL-Container'
`$env:ConnectionStrings__SqlConnectionString = '$conn'
Set-Location /workspace
dotnet run --project src/UI/Server --no-build --configuration Release --no-launch-profile --urls http://0.0.0.0:$script:ServePort *>> $script:AppLog
"@
    tmux kill-session -t app 2>$null
    tmux new-session -d -s app "pwsh -NoProfile -File $serveScript"
    Write-Info "App starting on 0.0.0.0:$script:ServePort (Release --no-build, Development, SQL Server)"

    # Surface app startup to stdout (kubectl exec is unreliable on Docker Desktop).
    $healthy = $false
    for ($i = 0; $i -lt 36; $i++) {
        try {
            $r = Invoke-WebRequest -UseBasicParsing "http://localhost:$script:ServePort/" -TimeoutSec 4
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { $healthy = $true; break }
        } catch { }
        Start-Sleep -Seconds 5
    }
    if ($healthy) {
        Write-Success "App answered on :$script:ServePort"
        Write-StructuredLog -Level "INFO" -Message "Serve app up" -Data @{ port = $script:ServePort; issue_number = $Issue }
    } else {
        Write-Info "App not yet answering on :$script:ServePort - recent app.log:"
        Get-Content $script:AppLog -Tail 25 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  [app] $_" }
    }
}

$script:TermPort = 7682

# Start a browser terminal (ttyd) attached to the LIVE 'agent' tmux session (the
# running Claude Code instance) and expose it via a SECOND Cloudflare quick
# tunnel, so a human can open a real terminal to the active session. Protected
# by per-run HTTP basic auth (the tunnel URL is unguessable but not secret, and
# this is a full shell in a privileged container holding credentials). Enabled
# unless TERMINAL_ENABLED=false.
function Start-Terminal {
    param([string]$Pr = "pending", [string]$Issue)
    if ($env:TERMINAL_ENABLED -eq "false") { Write-Info "TERMINAL_ENABLED=false - skipping browser terminal."; return }

    # Auth via an unguessable base-path token (capability URL), NOT HTTP basic
    # auth: ttyd's --credential guards the WebSocket upgrade with basic auth, and
    # Safari (incl. iOS) does not send basic-auth on WS upgrades, so the terminal
    # fails to connect there ("Press ENTER to reconnect"). A secret base path
    # needs no auth header, works in every browser, and is the same secret-URL
    # model as the app tunnel (plus the tunnel host is itself unguessable).
    $pathToken = -join ((48..57)+(97..122) | Get-Random -Count 28 | ForEach-Object { [char]$_ })
    $ttydLog = "/tmp/ttyd.log"
    $termTunnelLog = "/tmp/cloudflared-term.log"

    # ttyd runs `tmux attach -t agent` (-W = writable) so the browser terminal
    # shares the live Claude session with the Remote Control app. ttyd itself is
    # backgrounded in a tmux session, so its child inherits $TMUX and a plain
    # `tmux attach` would refuse to nest - `env -u TMUX` clears it so the attach
    # to the 'agent' session works. -b /<token> serves under the secret path.
    tmux kill-session -t ttyd 2>$null
    tmux new-session -d -s ttyd "ttyd -p $script:TermPort -W -b /$pathToken env -u TMUX tmux attach -t agent >> $ttydLog 2>&1"
    Write-Info "ttyd terminal starting on :$script:TermPort (attaches to the live 'agent' Claude session)"

    # Second Cloudflare quick tunnel for the terminal port (separate from the app tunnel).
    tmux has-session -t termtunnel 2>$null
    if ($LASTEXITCODE -ne 0) {
        tmux new-session -d -s termtunnel "cloudflared tunnel --no-autoupdate --url http://localhost:$script:TermPort > $termTunnelLog 2>&1"
        Write-Info "Cloudflare terminal tunnel starting -> :$script:TermPort"
    }
    $termUrl = $null
    for ($i = 0; $i -lt 60; $i++) {
        $raw = Get-Content $termTunnelLog -Raw -ErrorAction SilentlyContinue
        if ($raw) {
            $m = [regex]::Match($raw, 'https://[a-z0-9-]+\.trycloudflare\.com')
            if ($m.Success) { $termUrl = $m.Value; break }
        }
        Start-Sleep -Seconds 2
    }
    if ($termUrl) {
        # Full capability URL includes the secret base path (trailing slash matters).
        $fullUrl = "$termUrl/$pathToken/"
        Write-Success "LIVE TERMINAL URL: $fullUrl  (no login needed; the secret path IS the credential - keep it private)"
        Write-Host "AI_FACTORY_TERMINAL_URL issue=$Issue pr=$Pr url=$fullUrl"
        Write-StructuredLog -Level "SUCCESS" -Message "Browser terminal live via Cloudflare tunnel" -Data @{
            issue_number = $Issue; pr_number = $Pr; terminal_url = $fullUrl; port = $script:TermPort
        }
        # For the agent to report it in-session (see the prompt).
        Set-Content -Path "/tmp/terminal-url.txt" -Value $fullUrl -NoNewline -ErrorAction SilentlyContinue
        Publish-TerminalAnnotation -Url $fullUrl -Issue $Issue
    } else {
        Write-Failure "Could not obtain Cloudflare terminal tunnel URL (see $termTunnelLog)"
    }
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
    
    if (Test-Path "/var/agent-secrets/gh_token") {
        Get-Content "/var/agent-secrets/gh_token" | gh auth login --with-token
        Write-Success "GitHub CLI authenticated"
    } else {
        throw "GitHub token secret not found at /run/secrets/gh_token"
    }
    
    # 3. Configure git with GitHub token authentication
    Write-Progress "Configuring git..."
    git config --global user.name "AI Factory Bot"
    git config --global user.email "ai-factory-bot@users.noreply.github.com"
    git config --global init.defaultBranch master
    # The clone lands in an emptyDir owned by root (fsGroup makes it writable by
    # the non-root user, but the dir owner is still root); tell git the workspace
    # is trusted so it does not abort with "detected dubious ownership".
    git config --global --add safe.directory '*'
    
    # Configure git to use GitHub token for HTTPS authentication
    $ghToken = Get-Content "/var/agent-secrets/gh_token" -Raw
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
    # Also drop Playwright's `--with-deps` from the acceptance-test browser
    # install: the OS deps + chromium are already baked into the image, and
    # `--with-deps` shells out to sudo/apt which the pod's securityContext
    # (allowPrivilegeEscalation=false / no-new-privileges) blocks. Without the
    # flag, `playwright install chromium` sees the pre-baked browser and no-ops.
    if (Test-Path "./build.ps1") {
        (Get-Content "./build.ps1") `
            -replace '\$verbosity = "quiet"', '$verbosity = "normal"' `
            -replace 'install chromium --with-deps', 'install chromium' |
            Set-Content "./build.ps1"
        Write-Info "Build verbosity set to normal; Playwright --with-deps removed (browsers pre-baked)"
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

    # Configurable model + reasoning effort for the claude agent. Default:
    # Claude Sonnet 5 at low effort. Override with AI_MODEL / AI_MODEL_EFFORT
    # (plumbed from implement-issue.ps1 --model/--effort).
    $aiModel  = if ($env:AI_MODEL)        { $env:AI_MODEL }        else { "claude-sonnet-5" }
    $aiEffort = if ($env:AI_MODEL_EFFORT) { $env:AI_MODEL_EFFORT.ToLower() } else { "low" }
    Write-Info "AI model: $aiModel (effort: $aiEffort)"
    Write-StructuredLog -Level "INFO" -Message "Selected AI model" -Data @{ model = $aiModel; effort = $aiEffort }

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
            if (-not $env:BOBSHELL_API_KEY -and (Test-Path "/var/agent-secrets/bobshell_api_key")) {
                $bobKey = Get-Content "/var/agent-secrets/bobshell_api_key" -Raw
                if ($bobKey) { $env:BOBSHELL_API_KEY = $bobKey.Trim() }
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
            if (-not $env:ANTHROPIC_API_KEY -and (Test-Path "/var/agent-secrets/anthropic_api_key")) {
                # The key file is present but may be empty (OAuth-creds path):
                # Get-Content -Raw returns $null for an empty file, so guard the Trim.
                $apiKey = Get-Content "/var/agent-secrets/anthropic_api_key" -Raw
                if ($apiKey) { $env:ANTHROPIC_API_KEY = $apiKey.Trim() }
            }
            if (Test-Path "/var/agent-secrets/claude_credentials") {
                New-Item -ItemType Directory -Force -Path "/home/bobagent/.claude" | Out-Null
                Copy-Item "/var/agent-secrets/claude_credentials" "/home/bobagent/.claude/.credentials.json" -Force
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

            # Claude owns the implement -> build -> test -> fix loop: it runs the
            # SAME quality gates the pipeline enforces and fixes any failures in
            # this session, rather than the entrypoint running them afterwards
            # (where a failure could only abort, not be repaired).
            $claudePrompt = @"
Implement GitHub issue #${issueNumber}: $issueTitle

Description:
$issueBody

Instructions:
- Analyze the requirements and implement the change in this repository (/workspace).
- Follow existing code patterns and architecture (read CLAUDE.md and AGENTS.md if present).
- Make only the code/file changes needed to satisfy the issue. Keep it focused.
- VERIFY your work by running BOTH quality gates from the repo root:
    pwsh ./PrivateBuild.ps1
    pwsh ./AcceptanceTests.ps1
  These are the same gates CI enforces (compile, unit + integration tests, then
  Playwright acceptance tests, in SQLite mode). If EITHER fails, read the
  output, fix the code, and re-run that gate until it passes. Do not stop until
  BOTH gates pass.
- Do NOT run any git commands (no commit, push, branch, or PR), and do NOT edit
  build.ps1 / PrivateBuild.ps1 / AcceptanceTests.ps1 - the surrounding
  automation owns git and the build scripts.

LIVE PREVIEW URL - tell the user where to view the running app:
  A public Cloudflare tunnel serves a live preview of this app. The surrounding
  automation writes its URL to /tmp/live-url.txt once the tunnel is up (usually
  within the first minute or two). As one of your FIRST actions, run:
    cat /tmp/live-url.txt
  If it is empty, wait briefly and retry a couple of times. Once it returns a
  URL, report it to the user clearly, e.g.:
    "Live preview of the app: <url>  (fully loads after the quality gates pass)"
  Repeat this URL in your final summary so whoever is watching this session
  knows exactly where to view the running app.

BROWSER TERMINAL - tell the user how to open a terminal into this session:
  A second Cloudflare tunnel exposes a browser terminal attached to THIS live
  session. The automation writes its URL to /tmp/terminal-url.txt shortly after
  startup. Once available, run:
    cat /tmp/terminal-url.txt
  and report it to the user, e.g.:
    "Browser terminal (this session): <url>"
  The URL contains a secret path token (no login prompt) - opening it drops the
  user into a real terminal in this container, so treat the full URL as a
  secret. Include it in your final summary alongside the app URL.

ENDING THE SESSION - the app stays live for inspection after your work is done.
  When (and only when) the user explicitly says they are finished and want to
  end the session, run exactly:
    touch /tmp/session-done
  That tells the automation to shut down the app + tunnel and end the session.
  Do NOT run it on your own - wait for the user to ask. Mention this option to
  the user when you report the live preview URL (e.g. "tell me when you're done
  and I'll end the session").

IMPORTANT - completion signal: ONLY after BOTH PrivateBuild.ps1 and
AcceptanceTests.ps1 have passed, run this exact command as your final action:
  touch $sentinel
Run it exactly once, and only when both gates are green.
"@
            Set-Content -Path $promptFile -Value $claudePrompt -NoNewline

            # Launch claude DIRECTLY in a detached tmux session so its stdout is
            # the tmux pty. Remote Control needs an interactive pty; piping the
            # output (e.g. | tee) makes stdout a pipe and RC never registers.
            # Capture output for docker logs via `tmux pipe-pane` instead.
            $claudeCmd = "cd /workspace && claude --model `"$aiModel`" --remote-control `"$rcName`" --dangerously-skip-permissions `"`$(cat $promptFile)`""
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

            # Per design: bring the app + dev tunnel up NOW, in parallel with the
            # agent's implement/build/test loop, so the live URL is available
            # before the agent runs AcceptanceTests.ps1 (not only after the PR).
            # The app is restarted on the final code once the agent completes.
            if ($env:KEEP_ALIVE -eq "true") {
                Write-Progress "KEEP_ALIVE=true - opening Cloudflare tunnel in parallel with the agent (app serves after gates compile it)..."
                Start-Tunnel -Pr "pending" -Issue $issueNumber | Out-Null
            }

            # Bring the browser terminal up NOW - as soon as the live 'agent'
            # tmux/Claude session exists - so a human can attach while the agent
            # works, not only after the gates. Independent of KEEP_ALIVE.
            Start-Terminal -Pr "pending" -Issue $issueNumber

            # Poll for the completion sentinel (or session death / timeout). The
            # agent now also runs both quality gates and fixes failures in-session,
            # so allow more time than a bare implementation.
            $agentTimeoutMin = 45
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

    # 8b. Quality gates. For the claude agent these were run IN-SESSION by the
    #     agent (implement -> PrivateBuild -> AcceptanceTests -> fix until green),
    #     so the entrypoint does not re-run them. Other agents (e.g. bob) still
    #     have the entrypoint enforce the gates here before pushing.
    if ($aiAgent -eq "claude") {
        Write-Info "Quality gates were run in-session by the claude agent; entrypoint skips re-running them."
        Write-StructuredLog -Level "INFO" -Message "All quality gates passed" -Data @{ runner = "agent" }
    }
    elseif ($env:RUN_QUALITY_GATES -eq "false") {
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

    # Keep transient SQLite DBs (created by the gate runs and serve mode) out of
    # the PR - they are not covered by .gitignore.
    Add-Content -Path "/workspace/.git/info/exclude" -Value "`n*.db`nserve.db`nChurchBulletin.db" -ErrorAction SilentlyContinue

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

    # 11b. KEEP_ALIVE serve mode. The app + Cloudflare tunnel were already brought
    #      up in parallel with the agent (before AcceptanceTests). Now that the
    #      gates are green and the PR exists, restart the app so the stable tunnel
    #      URL serves the FINAL implemented code, then hold the container open.
    if ($env:KEEP_ALIVE -eq "true") {
        Write-Progress "KEEP_ALIVE=true - serving final build (Release --no-build) behind the tunnel..."
        Write-StructuredLog -Level "INFO" -Message "Serving final build" -Data @{ issue_number = $issueNumber; pr_number = $prNumber }
        Start-ServeApp -Issue $issueNumber
        Start-Tunnel -Pr $prNumber -Issue $issueNumber | Out-Null
        # (The browser terminal was already started early, when the agent
        # session launched, and persists through the gates/serve.)

        # Hold the container open for inspection, but let the REMOTE USER end it.
        # Exit (tearing down app + tunnel) when ANY of:
        #   1. the user asks the agent to end -> agent runs `touch /tmp/session-done`
        #   2. the Remote Control 'agent' tmux session has died (user closed it)
        #   3. a max hold time elapses (safety net; KEEP_ALIVE_MAX_HOURS, default 4)
        $endSignal = "/tmp/session-done"
        Remove-Item $endSignal -Force -ErrorAction SilentlyContinue   # clear any stale signal
        $maxHours = if ($env:KEEP_ALIVE_MAX_HOURS) { [double]$env:KEEP_ALIVE_MAX_HOURS } else { 4 }
        $holdDeadline = (Get-Date).AddHours($maxHours)
        Write-Info "Container held open (KEEP_ALIVE). Tell the agent to end the session, close the Remote Control session, or wait up to $maxHours h."
        Write-StructuredLog -Level "INFO" -Message "Entering KEEP_ALIVE hold" -Data @{ issue_number = $issueNumber; pr_number = $prNumber; max_hours = $maxHours }

        $endReason = $null
        while (-not $endReason) {
            if (Test-Path $endSignal)          { $endReason = "user-requested (session-done signal)" ; break }
            tmux has-session -t agent 2>$null
            if ($LASTEXITCODE -ne 0)           { $endReason = "remote-control session closed" ; break }
            if ((Get-Date) -gt $holdDeadline)  { $endReason = "max hold time ($maxHours h) reached" ; break }
            Start-Sleep -Seconds 10
        }

        Write-Success "KEEP_ALIVE ending: $endReason - tearing down app + tunnel."
        Write-StructuredLog -Level "INFO" -Message "KEEP_ALIVE ended" -Data @{ issue_number = $issueNumber; pr_number = $prNumber; reason = $endReason }
        tmux kill-session -t app        2>$null
        tmux kill-session -t tunnel     2>$null
        tmux kill-session -t ttyd       2>$null
        tmux kill-session -t termtunnel 2>$null
        tmux kill-session -t agent      2>$null
        exit 0
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