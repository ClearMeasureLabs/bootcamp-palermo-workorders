# AI Factory Executor - Usage Examples

## Basic Usage

### From Bob Shell

```
User: Run the AI Factory

Bob: Starting AI Factory Executor...

Discovering issues from GitHub Projects...
Found 5 issues in "Development" column with "AI Factory" label

Queue initialized:
- Pending: 5 issues
- Max concurrent: 2 subagents

Starting implementation...
→ Spawning Subagent 1 for Issue #123: Add user authentication
→ Spawning Subagent 2 for Issue #124: Implement search feature

## AI Factory Progress
Pending: 3 | In Progress: 2 | Completed: 0 | Failed: 0

Currently Working:
  - Issue #123: Add user authentication (2.5m elapsed)
  - Issue #124: Implement search feature (1.2m elapsed)

✓ Issue #123 complete - PR #456 (all checks green)
→ Spawning Subagent 1 for Issue #125: Add logging

[... continues until all issues processed ...]

## AI Factory Execution Complete

Summary:
  Total Issues: 5
  Completed: 4 (PRs ready for review)
  Failed: 1

Completed PRs:
- #456: Add user authentication (Issue #123)
- #457: Implement search feature (Issue #124)
- #458: Add logging (Issue #125)
- #459: Fix login bug (Issue #126)

Failed Issues:
- Issue #127: Complex refactoring - Error: Merge conflicts with master

Next Steps:
- Review and merge completed PRs
- Manually address Issue #127
```

### From Command Line

```bash
# Basic execution
pwsh .bob/skills/ai-factory-executor/executor.ps1

# Dry run to preview
pwsh .bob/skills/ai-factory-executor/executor.ps1 -DryRun

# Custom concurrency
pwsh .bob/skills/ai-factory-executor/executor.ps1 -MaxConcurrent 3

# Specific project
pwsh .bob/skills/ai-factory-executor/executor.ps1 -ProjectNumber 1

# Different repo
pwsh .bob/skills/ai-factory-executor/executor.ps1 -Org "MyOrg" -Repo "MyRepo"
```

## Test Discovery First

Before running the full executor, test issue discovery:

```bash
pwsh .bob/skills/ai-factory-executor/test-discovery.ps1
```

Output:
```
=== AI Factory Discovery Test ===

✓ GitHub CLI OK
✓ GitHub authentication OK
✓ Auto-detecting repository... ClearMeasureLabs/bootcamp-palermo-workorders
✓ Querying issues with 'AI Factory' label... Found 5 issues

Issues with 'AI Factory' label:

  Issue #123: Add user authentication
    Status: READY
    URL: https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/123
    Preview: Implement JWT-based authentication for the API. Requirements: - Add login endpoint...

  Issue #124: Implement search feature
    Status: READY
    URL: https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/124
    Preview: Add full-text search across work orders. Should support: - Search by title...

  Issue #125: Add logging
    Status: HAS PR #458
    URL: https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/125
    Preview: Add structured logging using Serilog...

Summary:
  Total issues with 'AI Factory' label: 5
  Ready to implement (no PR): 2
  Already have PRs: 3

✓ Ready to run AI Factory Executor on 2 issue(s)

Run: pwsh .bob/skills/ai-factory-executor/executor.ps1
```

## Workflow Integration

### Pre-requisites Setup

```bash
# Install GitHub CLI
winget install --id GitHub.cli

# Authenticate
gh auth login

# Verify access
gh repo view ClearMeasureLabs/bootcamp-palermo-workorders
```

### Creating AI Factory Issues

1. Create a new issue in GitHub
2. Add detailed description with acceptance criteria
3. Add label: "AI Factory"
4. Add to GitHub Project
5. Move to "Development" column

Example issue template:
```markdown
## Description
Implement user authentication using JWT tokens.

## Acceptance Criteria
- [ ] Add login endpoint at POST /api/auth/login
- [ ] Accept username and password
- [ ] Return JWT token on success
- [ ] Add authentication middleware
- [ ] Protect existing endpoints with [Authorize]
- [ ] Add unit tests for auth service
- [ ] Add integration tests for login endpoint

## Technical Notes
- Use System.IdentityModel.Tokens.Jwt
- Store tokens in HttpOnly cookies
- Token expiry: 1 hour
- Follow existing patterns in CLAUDE.md

## Definition of Done
- All tests pass
- No security vulnerabilities (trufflehog clean)
- StyleCop clean
- PR created and all checks green
```

### Monitoring Progress

While the executor runs, you can:

```bash
# Check PR status
gh pr list --label "AI Factory"

# View specific PR
gh pr view 456

# Check PR checks
gh pr checks 456

# View PR diff
gh pr diff 456
```

### After Completion

```bash
# Review completed PRs
gh pr list --label "AI Factory" --state open

# Merge a PR (manual approval)
gh pr merge 456 --squash --delete-branch

# Or review in browser
gh pr view 456 --web
```

## Troubleshooting Examples

### No Issues Found

```
ℹ No issues found with 'AI Factory' label

To test this skill, create an issue with:
  1. Label: 'AI Factory'
  2. Add to a GitHub Project
  3. Move to 'Development' column
```

**Solution:** Create test issues or verify label spelling.

### Authentication Failed

```
✗ GitHub CLI not authenticated
Run: gh auth login
```

**Solution:** Run `gh auth login` and follow prompts.

### Subagent Timeout

```
✗ Issue #123 failed: Subagent timeout after 60 minutes
```

**Solution:** Issue may be too complex. Break into smaller issues or increase timeout.

### Merge Conflicts

```
✗ Issue #124 failed: Merge conflicts with master
```

**Solution:** Manually resolve conflicts or update issue to account for recent changes.

## Advanced Scenarios

### Parallel Execution with High Concurrency

```bash
# Process 5 issues simultaneously (requires more resources)
pwsh .bob/skills/ai-factory-executor/executor.ps1 -MaxConcurrent 5
```

### Continuous Mode (Process New Issues as They Arrive)

```bash
# Run in loop, checking every 5 minutes
while ($true) {
    pwsh .bob/skills/ai-factory-executor/executor.ps1
    Start-Sleep -Seconds 300
}
```

### Integration with CI/CD

```yaml
# .github/workflows/ai-factory.yml
name: AI Factory Executor

on:
  schedule:
    - cron: '0 */6 * * *'  # Every 6 hours
  workflow_dispatch:

jobs:
  execute:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run AI Factory
        run: |
          pwsh .bob/skills/ai-factory-executor/executor.ps1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## See Also

- [SKILL.md](SKILL.md) - Complete skill specification
- [README.md](README.md) - Setup and configuration
- [INTEGRATION.md](INTEGRATION.md) - Subagent integration guide
- [executor.ps1](executor.ps1) - Main implementation
