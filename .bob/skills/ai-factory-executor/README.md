# AI Factory Executor

Autonomous implementation of GitHub issues labeled "AI Factory" using isolated Docker containers or direct execution.

## Overview

The AI Factory Executor discovers GitHub issues with the "AI Factory" label and autonomously implements them using one of two modes:

1. **Docker Mode** (Recommended): Spawns isolated Docker containers for each issue, providing full isolation and parallel execution capability
2. **Direct Mode** (Fallback): Implements issues directly in the current session when Docker is unavailable

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Host: Main Agent                          │
│  ┌────────────────────────────────────────────────────┐     │
│  │         AI Factory Executor (executor.ps1)         │     │
│  │  - Discovers GitHub issues                         │     │
│  │  - Spawns Docker containers (or direct exec)       │     │
│  │  - Monitors container status                       │     │
│  │  - Aggregates results                              │     │
│  └────────────────────────────────────────────────────┘     │
│                           │                                  │
│              ┌────────────┼────────────┐                     │
│              ▼            ▼            ▼                     │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐        │
│  │ Container 1  │ │ Container 2  │ │ Container N  │        │
│  │ Issue #6936  │ │ Issue #6937  │ │ Issue #XXXX  │        │
│  │              │ │              │ │              │        │
│  │ Bob CLI      │ │ Bob CLI      │ │ Bob CLI      │        │
│  │ + gh auth    │ │ + gh auth    │ │ + gh auth    │        │
│  │ + git config │ │ + git config │ │ + git config │        │
│  │ + .NET 10    │ │ + .NET 10    │ │ + .NET 10    │        │
│  │ + PowerShell │ │ + PowerShell │ │ + PowerShell │        │
│  └──────────────┘ └──────────────┘ └──────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Prerequisites

### Required
- **PowerShell 7+**: For script execution
- **GitHub CLI (gh)**: Authenticated with `repo` and `workflow` scopes
- **Git**: For repository operations

### Optional (for Docker Mode)
- **Docker**: For containerized execution
- **Docker Compose**: For advanced orchestration (optional)

## Installation

1. Ensure prerequisites are installed:
```powershell
# Check PowerShell version
$PSVersionTable.PSVersion  # Should be 7.0+

# Check GitHub CLI
gh --version
gh auth status

# Check Git
git --version

# Check Docker (optional)
docker --version
```

2. Authenticate GitHub CLI:
```powershell
gh auth login
# Select: GitHub.com
# Select: HTTPS
# Authenticate in browser
```

## Usage

### Basic Usage

Run the executor from the repository root:

```powershell
pwsh .bob/skills/ai-factory-executor/executor.ps1
```

### Dry Run

Preview what would be executed without making changes:

```powershell
pwsh .bob/skills/ai-factory-executor/executor.ps1 -DryRun
```

### Custom Parameters

```powershell
pwsh .bob/skills/ai-factory-executor/executor.ps1 `
    -Org "ClearMeasureLabs" `
    -Repo "bootcamp-palermo-workorders" `
    -MaxConcurrent 3 `
    -PollInterval 15
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MaxConcurrent` | int | 2 | Maximum concurrent containers (Docker mode only) |
| `Org` | string | auto-detect | GitHub organization |
| `Repo` | string | auto-detect | GitHub repository |
| `PollInterval` | int | 10 | Status check interval in seconds |
| `DryRun` | switch | false | Preview mode without execution |

## Execution Modes

### Docker Mode (Recommended)

When `container-manager.ps1` is present, the executor runs in Docker mode:

**Advantages:**
- Full isolation per issue
- Parallel execution capability
- Clean environment for each implementation
- Automatic cleanup
- Comprehensive logging

**Requirements:**
- Docker daemon running
- Sufficient system resources (2 CPU cores, 4GB RAM per container)

**Container Lifecycle:**
1. Build agent image (first run only)
2. Inject GitHub credentials
3. Spawn container with issue context
4. Monitor container logs
5. Wait for completion
6. Cleanup container and credentials

### Direct Mode (Fallback)

When Docker is unavailable, the executor falls back to direct execution:

**Behavior:**
- Creates feature branch locally
- Outputs implementation task
- Exits for manual implementation
- Requires re-run after implementation

**Workflow:**
1. Run executor → creates branch and outputs task
2. Implement the task manually
3. Commit and push changes
4. Create PR manually
5. Re-run executor for next issue

## Docker Container Details

### Image: `ai-factory-agent:latest`

**Base:** `mcr.microsoft.com/dotnet/sdk:10.0`

**Includes:**
- .NET SDK 10.0
- PowerShell 7+
- Git
- GitHub CLI (gh)
- Bob CLI (when available)

**Security:**
- Non-root user (`bobagent`)
- Read-only credential mounts
- Resource limits (2 CPU, 4GB RAM)
- Automatic credential cleanup

### Container Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ISSUE_NUMBER` | GitHub issue number | `6936` |
| `ISSUE_TITLE` | Issue title | `Demo Issue #4` |
| `ISSUE_BODY` | Issue description | Full markdown content |
| `ISSUE_URL` | Issue URL | `https://github.com/...` |
| `BRANCH_NAME` | Feature branch name | `feature/issue-6936-demo-issue-4` |
| `REPO_ORG` | Repository organization | `ClearMeasureLabs` |
| `REPO_NAME` | Repository name | `bootcamp-palermo-workorders` |

### Container Logs

Logs are saved to `.bob/skills/ai-factory-executor/logs/issue-{number}.log`

**Log Format:** Structured JSON lines for parsing
```json
{"timestamp":"2026-07-17T18:30:00Z","level":"INFO","message":"Cloning repository..."}
{"timestamp":"2026-07-17T18:30:15Z","level":"INFO","message":"Branch created","branch_name":"feature/issue-6936-demo-issue-4"}
{"timestamp":"2026-07-17T18:35:00Z","level":"SUCCESS","message":"All checks passed","pr_number":1234,"issue_number":6936}
```

## Issue Discovery

The executor discovers issues using the following criteria:

1. **Label:** Must have "AI Factory" label
2. **State:** Must be open
3. **No PR:** Must not have an existing pull request
4. **No Branch:** Must not have an existing feature branch

### Branch Naming Convention

`feature/issue-{number}-{slug}`

Example: `feature/issue-6936-demo-issue-4`

## Implementation Workflow

### Docker Mode Workflow

1. **Discovery:** Find eligible issues
2. **Spawn:** Create Docker container with credentials
3. **Clone:** Container clones repository
4. **Branch:** Container creates feature branch
5. **Implement:** Bob CLI implements the issue
6. **Test:** Run quality checks (tests, linting, security)
7. **Commit:** Commit changes with conventional commit message
8. **Push:** Push branch to remote
9. **PR:** Create pull request
10. **Monitor:** Wait for CI/CD checks to pass
11. **Report:** Log success/failure
12. **Cleanup:** Remove container and credentials

### Direct Mode Workflow

1. **Discovery:** Find eligible issues
2. **Branch:** Create feature branch locally
3. **Output:** Display implementation task
4. **Exit:** Wait for manual implementation
5. **Manual:** User implements, commits, pushes, creates PR
6. **Re-run:** User re-runs executor for next issue

## Quality Checks

Each implementation includes automatic quality checks:

- **Secrets Scanning:** TruffleHog before commit
- **Code Style:** StyleCop for C# changes
- **Security:** npm audit for package.json changes
- **Tests:** Run test suite before commit
- **Build:** Verify successful build

## PR Monitoring

The executor monitors PRs until all checks pass:

- **Check Interval:** 30 seconds (configurable)
- **Timeout:** 30 minutes (configurable)
- **Status Tracking:** Pending, Success, Failed, Skipped
- **Failure Handling:** Report failed checks and exit

## Troubleshooting

### Docker Mode Issues

**Problem:** Container fails to start
```
Solution: Check Docker daemon is running
docker ps
sudo systemctl start docker  # Linux
```

**Problem:** Permission denied on Docker socket
```
Solution: Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

**Problem:** Out of disk space
```
Solution: Clean up Docker resources
docker system prune -a
```

### Authentication Issues

**Problem:** GitHub CLI not authenticated
```
Solution: Re-authenticate
gh auth login
gh auth status
```

**Problem:** Invalid token
```
Solution: Refresh token
gh auth refresh
```

### Build Issues

**Problem:** Docker image build fails
```
Solution: Check Dockerfile.agent syntax
docker build -t ai-factory-agent:latest -f Dockerfile.agent .
```

## File Structure

```
.bob/skills/ai-factory-executor/
├── Dockerfile.agent              # Docker image for agents
├── docker-compose.agent.yml      # Compose configuration (optional)
├── agent-entrypoint.ps1          # Container entrypoint script
├── container-manager.ps1         # Container lifecycle management
├── inject-credentials.ps1        # Credential injection helper
├── pr-monitor.ps1                # PR status monitoring
├── executor.ps1                  # Main orchestrator
├── test-discovery.ps1            # Issue discovery testing
├── README.md                     # This file
├── DOCKER-AGENT-PLAN.md          # Architecture documentation
└── logs/                         # Container execution logs
    ├── issue-6936.log
    └── ...
```

## Configuration

### Environment Variables

```powershell
# Maximum concurrent containers
$env:MAX_CONCURRENT_AGENTS = 3

# Container timeout in minutes
$env:AGENT_TIMEOUT_MINUTES = 45

# Docker image name
$env:DOCKER_IMAGE = "ai-factory-agent:latest"
```

### Docker Compose

For advanced orchestration, use `docker-compose.agent.yml`:

```powershell
docker-compose -f docker-compose.agent.yml up
```

## Security Considerations

1. **Credentials:** Stored as Docker secrets, never in environment variables
2. **Isolation:** Each container runs as non-root user
3. **Cleanup:** Automatic removal of credentials after use
4. **Scanning:** TruffleHog scans before every commit
5. **Audit:** All actions logged with timestamps

## Performance

### Resource Requirements

**Per Container:**
- CPU: 2 cores
- Memory: 4GB
- Disk: 10GB (for repository and build artifacts)

**Recommended System:**
- CPU: 8+ cores
- Memory: 16GB+
- Disk: 50GB+ free space

### Execution Times

- **Container Startup:** ~10 seconds
- **Repository Clone:** ~30 seconds
- **Implementation:** 5-15 minutes (varies by issue)
- **PR Creation:** ~5 seconds
- **CI/CD Checks:** 5-20 minutes (varies by pipeline)

**Total per Issue:** 10-35 minutes

## Examples

### Example 1: Process All Issues

```powershell
# Run executor to process all eligible issues
pwsh .bob/skills/ai-factory-executor/executor.ps1
```

**Output:**
```
=== AI Factory Executor ===

✓ Prerequisites OK
ℹ Detected: ClearMeasureLabs/bootcamp-palermo-workorders
ℹ Working with issue labels (skipping Projects v2 API)
→ Discovering issues with 'AI Factory' label...
✓ Found 4 eligible issues
ℹ Starting sequential implementation of 4 issues (Docker container mode)

## Processing Issue #6936
Title: Demo Issue #4
→ Starting Docker container for Issue #6936...
✓ Container started: ai-factory-issue-6936-1234
→ Monitoring container for Issue #6936...
✓ All PR checks passed!
✓ Issue #6936 implemented successfully
✓ Container stopped and removed
✓ Issue #6936 complete

...

## AI Factory Execution Complete

Summary:
  Total Issues: 4
  Completed: 4
  Skipped: 0
  Failed: 0
```

### Example 2: Dry Run

```powershell
# Preview what would be executed
pwsh .bob/skills/ai-factory-executor/executor.ps1 -DryRun
```

**Output:**
```
=== AI Factory Executor ===

✓ Prerequisites OK
ℹ Detected: ClearMeasureLabs/bootcamp-palermo-workorders
✓ Found 4 eligible issues
ℹ [DRY RUN] Would process 4 issues:
  - Issue #6936: Demo Issue #4
  - Issue #6937: Demo Issue #5
  - Issue #6938: Demo Issue #6
  - Issue #6939: Demo Issue #7
```

### Example 3: Direct Mode (No Docker)

```powershell
# Remove container-manager.ps1 to force direct mode
mv container-manager.ps1 container-manager.ps1.bak

# Run executor
pwsh .bob/skills/ai-factory-executor/executor.ps1
```

**Output:**
```
=== AI Factory Executor ===

✓ Prerequisites OK
ℹ Starting sequential implementation of 4 issues (Direct mode)

## Processing Issue #6936
Title: Demo Issue #4
→ Creating branch: feature/issue-6936-demo-issue-4

=== TASK FOR IMPLEMENTATION ===
Implement GitHub issue #6936: Demo Issue #4
...
=== END TASK ===

Please implement the above task in the current Bob Shell session.
After implementation, commit, push, and create PR, then re-run this executor.
```

## Contributing

To enhance the AI Factory Executor:

1. **Add Features:** Modify `executor.ps1` or create new modules
2. **Improve Container:** Update `Dockerfile.agent` or `agent-entrypoint.ps1`
3. **Add Monitoring:** Extend `pr-monitor.ps1` with new checks
4. **Test:** Use `test-discovery.ps1` for testing issue discovery

## Support

For issues or questions:

1. Check logs in `.bob/skills/ai-factory-executor/logs/`
2. Review container logs: `docker logs <container-id>`
3. Run with `-Verbose` flag for detailed output
4. Consult `DOCKER-AGENT-PLAN.md` for architecture details

## License

Same as parent repository.