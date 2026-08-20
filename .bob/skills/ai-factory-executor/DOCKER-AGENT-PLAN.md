# Docker-Based AI Factory Agent Plan

## Overview

Transform the AI Factory Executor to use isolated Docker containers for each GitHub issue implementation. Each container will run a Bob CLI agent session with full repository access, credentials, and tooling to autonomously implement issues from branch creation through PR approval.

## Current State Analysis

### Existing Components
- **executor.ps1**: Main orchestrator that discovers issues and manages workflow
- **Dockerfile**: Basic multi-stage build (exists but needs enhancement)
- **docker-compose.yml**: Basic compose configuration
- **GitHub CLI (gh)**: Authenticated in host session
- **Bob CLI**: Available in host session with configuration

### Current Limitations
1. No credential injection mechanism for containers
2. No container orchestration for parallel issue processing
3. No PR monitoring until green build status
4. No container-to-main-agent communication
5. Manual implementation required (not autonomous)

## Architecture Design

### Container Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Host: Main Agent                          │
│  ┌────────────────────────────────────────────────────┐     │
│  │         AI Factory Executor (executor.ps1)         │     │
│  │  - Discovers GitHub issues                         │     │
│  │  - Spawns Docker containers                        │     │
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
│         │                │                │                  │
│         └────────────────┴────────────────┘                  │
│                          │                                   │
│                          ▼                                   │
│              ┌────────────────────────┐                      │
│              │   GitHub Repository    │                      │
│              │  - Clone               │                      │
│              │  - Branch              │                      │
│              │  - Commit              │                      │
│              │  - Push                │                      │
│              │  - Create PR           │                      │
│              │  - Monitor CI          │                      │
│              └────────────────────────┘                      │
└─────────────────────────────────────────────────────────────┘
```

### Container Lifecycle

1. **Spawn**: Main agent creates container with issue context
2. **Initialize**: Container sets up credentials, clones repo
3. **Implement**: Bob CLI agent implements the issue
4. **Validate**: Run tests, quality checks
5. **Submit**: Create PR, push changes
6. **Monitor**: Watch CI/CD until all checks pass
7. **Report**: Send status back to main agent
8. **Cleanup**: Container exits, logs preserved

## Implementation Plan

### Phase 1: Enhanced Dockerfile

**File**: `.bob/skills/ai-factory-executor/Dockerfile.agent`

**Requirements**:
- Base: `mcr.microsoft.com/dotnet/sdk:10.0` (includes .NET 10)
- Install PowerShell 7+
- Install git
- Install GitHub CLI (gh)
- Install Bob CLI (download from releases or build)
- Install Docker CLI (for nested scenarios if needed)
- Set up working directory structure
- Configure entrypoint script

**Key Features**:
- Multi-stage build for smaller image
- Non-root user for security
- Volume mounts for credentials
- Environment variable injection
- Logging to stdout for container logs

### Phase 2: Credential Injection

**File**: `.bob/skills/ai-factory-executor/inject-credentials.ps1`

**Mechanism**:
1. Export GitHub CLI token from host: `gh auth token`
2. Export Bob CLI configuration from `~/.bob/` or equivalent
3. Mount as Docker secrets or environment variables
4. Container entrypoint configures gh and Bob CLI on startup

**Security Considerations**:
- Use Docker secrets (not environment variables for sensitive data)
- Temporary credential files deleted after container init
- Read-only mounts where possible
- No credentials in image layers

### Phase 3: Container Orchestration

**File**: `.bob/skills/ai-factory-executor/container-manager.ps1`

**Functions**:
- `Start-AgentContainer`: Spawn container with issue context
- `Get-ContainerStatus`: Poll container logs for status
- `Stop-AgentContainer`: Graceful shutdown
- `Get-ContainerLogs`: Retrieve execution logs
- `Wait-ContainerCompletion`: Block until done

**Features**:
- Parallel execution (configurable max concurrent)
- Resource limits (CPU, memory)
- Timeout handling
- Automatic cleanup on failure
- Log aggregation

### Phase 4: Issue Dispatch Logic

**File**: `.bob/skills/ai-factory-executor/agent-entrypoint.ps1`

**Container Entrypoint Script**:
```powershell
# 1. Receive issue context via environment variables
$issueNumber = $env:ISSUE_NUMBER
$issueTitle = $env:ISSUE_TITLE
$issueBody = $env:ISSUE_BODY
$issueUrl = $env:ISSUE_URL
$branchName = $env:BRANCH_NAME
$repoOrg = $env:REPO_ORG
$repoName = $env:REPO_NAME

# 2. Configure credentials
gh auth login --with-token < /run/secrets/gh_token
git config --global user.name "AI Factory Bot"
git config --global user.email "ai-factory-bot@users.noreply.github.com"

# 3. Clone repository
git clone "https://github.com/$repoOrg/$repoName.git" /workspace
cd /workspace

# 4. Create feature branch
git checkout -b $branchName

# 5. Invoke Bob CLI with task
$task = @"
Implement GitHub issue #$issueNumber: $issueTitle

**Issue URL:** $issueUrl

**Description:**
$issueBody

**Instructions:**
1. Implement the changes following the issue's acceptance criteria
2. Run tests before committing
3. Commit with message: "feat: $issueTitle (#$issueNumber)"
4. Push branch
5. Create PR
6. Monitor PR checks until all green
7. Report completion status

**Constraints:**
- Follow existing code patterns and architecture (see CLAUDE.md)
- Run trufflehog before pushing to check for secrets
- If C# changes, run stylecop
- If package.json changes, run npm audit
- If blocked or unclear, report back immediately
- Do not merge - only monitor until green
"@

# 6. Execute via Bob CLI
bob task execute --task "$task" --auto-commit --auto-push

# 7. Create PR
gh pr create --repo "$repoOrg/$repoName" \
  --title "$issueTitle" \
  --body "Closes #$issueNumber`n`n$issueBody" \
  --head $branchName \
  --base master

# 8. Monitor PR until green
$prNumber = gh pr view --json number --jq .number
while ($true) {
  $checks = gh pr checks $prNumber --json state --jq '.[] | select(.state != "SUCCESS") | .state'
  if (-not $checks) {
    Write-Host "✓ All PR checks passed"
    break
  }
  Write-Host "⏳ Waiting for PR checks... (pending: $($checks.Count))"
  Start-Sleep -Seconds 30
}

# 9. Report success
Write-Host "SUCCESS: Issue #$issueNumber implemented and PR checks green"
exit 0
```

### Phase 5: Modified Executor

**File**: `.bob/skills/ai-factory-executor/executor.ps1` (modifications)

**Changes**:
1. Replace `Invoke-IssueImplementation` with `Start-AgentContainer`
2. Add container status polling loop
3. Aggregate container results
4. Handle container failures gracefully
5. Cleanup containers after completion

**New Flow**:
```powershell
function Invoke-IssueImplementation {
    param($SubagentTask)
    
    $issue = $SubagentTask.Issue
    $branchName = $SubagentTask.BranchName
    
    # Start container
    $containerId = Start-AgentContainer `
        -IssueNumber $issue.number `
        -IssueTitle $issue.title `
        -IssueBody $issue.body `
        -IssueUrl $issue.url `
        -BranchName $branchName `
        -RepoOrg $script:Org `
        -RepoName $script:Repo
    
    # Monitor until completion
    $result = Wait-ContainerCompletion -ContainerId $containerId -TimeoutMinutes 30
    
    # Retrieve logs
    $logs = Get-ContainerLogs -ContainerId $containerId
    
    # Cleanup
    Stop-AgentContainer -ContainerId $containerId
    
    return $result
}
```

### Phase 6: PR Monitoring

**File**: `.bob/skills/ai-factory-executor/pr-monitor.ps1`

**Functions**:
- `Wait-PrChecks`: Poll PR status until all checks pass
- `Get-PrCheckStatus`: Query GitHub API for check runs
- `Get-PrReviewStatus`: Check for required reviews
- `Send-PrNotification`: Alert on failures

**Features**:
- Exponential backoff for API rate limiting
- Timeout handling (default 30 minutes)
- Detailed status reporting
- Failure analysis (which check failed, why)

### Phase 7: Container-to-Main Communication

**Mechanism**: Container logs + exit codes

**Status Reporting**:
- Container writes structured JSON logs to stdout
- Main agent parses logs for status updates
- Exit code indicates success/failure
- Final log line contains summary

**Log Format**:
```json
{"timestamp": "2026-07-17T18:30:00Z", "level": "INFO", "message": "Cloning repository..."}
{"timestamp": "2026-07-17T18:30:15Z", "level": "INFO", "message": "Branch created: feature/issue-6936-demo-issue-4"}
{"timestamp": "2026-07-17T18:31:00Z", "level": "INFO", "message": "Implementation complete"}
{"timestamp": "2026-07-17T18:31:30Z", "level": "INFO", "message": "PR created: #1234"}
{"timestamp": "2026-07-17T18:35:00Z", "level": "SUCCESS", "message": "All checks passed", "pr_number": 1234, "issue_number": 6936}
```

## File Structure

```
.bob/skills/ai-factory-executor/
├── Dockerfile.agent              # Enhanced Dockerfile for agent containers
├── docker-compose.agent.yml      # Compose file for agent orchestration
├── agent-entrypoint.ps1          # Container entrypoint script
├── container-manager.ps1         # Container lifecycle management
├── inject-credentials.ps1        # Credential injection helper
├── pr-monitor.ps1                # PR status monitoring
├── executor.ps1                  # Modified main orchestrator
├── test-discovery.ps1            # Existing discovery tool
├── README.md                     # Updated documentation
├── DOCKER-AGENT-PLAN.md          # This file
└── logs/                         # Container execution logs
    ├── issue-6936.log
    ├── issue-6937.log
    └── ...
```

## Configuration

**Environment Variables**:
- `MAX_CONCURRENT_AGENTS`: Maximum parallel containers (default: 2)
- `AGENT_TIMEOUT_MINUTES`: Container timeout (default: 30)
- `DOCKER_IMAGE`: Agent image name (default: ai-factory-agent:latest)
- `GITHUB_TOKEN`: GitHub API token (injected as secret)
- `BOB_CLI_CONFIG`: Bob CLI configuration (injected as volume)

**Docker Compose**:
```yaml
version: '3.8'

services:
  ai-factory-agent:
    build:
      context: .
      dockerfile: Dockerfile.agent
    image: ai-factory-agent:latest
    environment:
      - ISSUE_NUMBER
      - ISSUE_TITLE
      - ISSUE_BODY
      - ISSUE_URL
      - BRANCH_NAME
      - REPO_ORG
      - REPO_NAME
    secrets:
      - gh_token
    volumes:
      - bob_config:/root/.bob:ro
      - agent_workspace:/workspace
    networks:
      - ai-factory
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G

secrets:
  gh_token:
    external: true

volumes:
  bob_config:
    external: true
  agent_workspace:

networks:
  ai-factory:
    driver: bridge
```

## Testing Strategy

### Unit Tests
- Container spawn/stop
- Credential injection
- Log parsing
- Status reporting

### Integration Tests
1. **Single Issue Test**: Process one demo issue end-to-end
2. **Parallel Test**: Process 3 issues concurrently
3. **Failure Test**: Handle container crash gracefully
4. **Timeout Test**: Abort long-running containers
5. **PR Monitor Test**: Wait for CI checks to pass

### Acceptance Criteria
- [ ] Container spawns successfully with credentials
- [ ] Bob CLI executes within container
- [ ] Repository cloned and branch created
- [ ] Changes committed and pushed
- [ ] PR created automatically
- [ ] PR checks monitored until green
- [ ] Container reports success/failure
- [ ] Main agent aggregates results
- [ ] Logs preserved for debugging
- [ ] Multiple containers run in parallel

## Security Considerations

1. **Credential Management**:
   - Use Docker secrets (not env vars)
   - Rotate tokens regularly
   - Limit token scopes (repo, workflow)
   - No credentials in image layers

2. **Container Isolation**:
   - Non-root user
   - Read-only root filesystem where possible
   - Resource limits (CPU, memory)
   - Network isolation

3. **Code Execution**:
   - Validate issue content before execution
   - Sandbox Bob CLI execution
   - Timeout enforcement
   - Audit logging

## Rollout Plan

### Phase 1: Development (Week 1)
- Create Dockerfile.agent
- Implement container-manager.ps1
- Build credential injection

### Phase 2: Integration (Week 2)
- Modify executor.ps1
- Add PR monitoring
- Implement logging

### Phase 3: Testing (Week 3)
- Unit tests
- Integration tests
- Performance testing

### Phase 4: Deployment (Week 4)
- Documentation
- CI/CD integration
- Production rollout

## Success Metrics

- **Throughput**: Issues processed per hour
- **Success Rate**: % of issues successfully implemented
- **Time to Green**: Average time from issue to green PR
- **Container Efficiency**: Resource utilization
- **Error Rate**: % of container failures

## Future Enhancements

1. **Multi-Repository Support**: Process issues across multiple repos
2. **Priority Queue**: Prioritize critical issues
3. **Smart Retry**: Retry failed issues with different strategies
4. **Cost Optimization**: Spot instances, container reuse
5. **AI Review**: Automated code review before PR creation
6. **Metrics Dashboard**: Real-time monitoring UI
7. **Slack Integration**: Notifications for completions/failures
8. **Auto-Merge**: Merge PRs when all checks pass (opt-in)

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Container resource exhaustion | High | Resource limits, monitoring |
| Credential leakage | Critical | Docker secrets, audit logging |
| Infinite loops in Bob CLI | Medium | Timeout enforcement |
| GitHub API rate limits | Medium | Exponential backoff, caching |
| Network failures | Low | Retry logic, health checks |
| Docker daemon crash | High | Restart policy, monitoring |

## Conclusion

This plan transforms the AI Factory Executor into a fully autonomous, containerized system capable of processing GitHub issues in parallel with complete isolation and security. Each container runs an independent Bob CLI agent that implements issues from start to finish, including PR creation and monitoring until all checks pass.

The architecture is scalable, secure, and maintainable, with clear separation of concerns and comprehensive error handling. The implementation can be rolled out incrementally, with each phase building on the previous one.
