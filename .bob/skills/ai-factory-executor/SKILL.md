---
name: ai-factory-executor
description: 'Autonomous executor for GitHub Projects v2 issues labeled "AI Factory" in "Development" column. Implements issues on feature branches, submits PRs, monitors builds, and orchestrates parallel execution via subagents. Written as a single-file .NET 10 application.'
metadata:
  user-invocable: true
  disable-model-invocation: false
---

# AI Factory Executor

Autonomous implementation of GitHub Projects v2 issues marked for AI development. This skill acts as an orchestrator that:
1. Discovers ready-to-implement issues from GitHub Projects
2. Delegates implementation to subagents (parallel execution)
3. Monitors PR builds and manages the queue
4. Continues until all "AI Factory" issues in "Development" are complete

**Use when:** The user wants to "run the AI Factory", "implement AI Factory issues", "process the Development column", or "auto-implement ready issues".

## Architecture

```
Main Agent (Orchestrator)
├── Query GitHub Projects API
├── Maintain issue queue
├── Spawn Subagent 1 → Implement Issue A
│   ├── Create feature branch
│   ├── Implement changes
│   ├── Submit PR
│   └── Monitor until green
├── Spawn Subagent 2 → Implement Issue B (when slot available)
└── Report progress and completion
```

**Concurrency:** Max 2 subagents running simultaneously (configurable). Main agent remains free for user interaction.

**Implementation:** Single-file .NET 10 application (`executor.cs` + `executor.csproj`) for cross-platform execution with minimal dependencies.

## Prerequisites

- **.NET 10 SDK** - Required to build and run
- **GitHub CLI (`gh`)** - Installed and authenticated
- **Git** - Repository with remote configured
- **GitHub Projects v2** - Access for the organization
- **Subagent/Task tool** - Available (for delegation)

## Quick Start

```bash
# From the skill directory
cd .bob/skills/ai-factory-executor

# Build
dotnet build -c Release

# Test discovery first
dotnet run --project test-discovery.csproj

# Run the executor
dotnet run --project executor.csproj

# With options
dotnet run --project executor.csproj -- --dry-run --max-concurrent 3
```

## Steps

### 1. Discovery Phase

Query GitHub Projects v2 for eligible issues:

```bash
# Get issues with "AI Factory" label
gh issue list --repo <org>/<repo> \
  --label "AI Factory" \
  --state open \
  --json number,title,body,url,labels
```

**Filter criteria:**
- Label contains "AI Factory"
- Project column/status is "Development"
- Issue state is "OPEN"
- Not already assigned to a PR (check for linked PRs)

### 2. Queue Management

Maintain an in-memory queue of discovered issues:
- **Pending:** Issues ready to implement
- **In Progress:** Issues currently being worked on by subagents
- **Completed:** Issues with green PRs
- **Failed:** Issues that encountered errors

Track subagent slots (default: 2 max concurrent).

### 3. Subagent Delegation (per issue)

For each issue, spawn a subagent with this task:

```
Implement GitHub issue #<number>: <title>

**Issue URL:** <url>
**Description:**
<body>

**Instructions:**
1. Create feature branch: `feature/issue-<number>-<slug>`
2. Read the issue description carefully - it contains the full requirements
3. Implement the changes following the issue's acceptance criteria
4. Commit with message: "feat: <title> (#<number>)"
5. Push branch and create PR with:
   - Title: <issue title>
   - Body: "Closes #<number>\n\n<issue body>"
6. Monitor PR checks until all green
7. Report completion status

**Constraints:**
- Follow existing code patterns and architecture
- Run tests before pushing
- If blocked, report back immediately
- Do not merge - only monitor until green
```

**Subagent execution:**
- Use Task tool if available (preferred for isolation)
- Subagent should use `checkin-dance` skill internally for PR workflow
- Subagent reports back: `{ "status": "success|failed", "pr_url": "...", "issue": <number> }`

### 4. Orchestration Loop

Main agent loop (runs until queue is empty):

```csharp
while (pending.Count > 0 || inProgress.Count > 0)
{
    // Start new work if slots available
    if (inProgress.Count < MaxConcurrent && pending.Count > 0)
    {
        issue = pending.Dequeue();
        subagent = SpawnSubagent(issue);
        inProgress.Add(subagent);
    }
    
    // Check subagent status
    foreach (subagent in inProgress)
    {
        status = CheckSubagentStatus(subagent);
        if (status.Complete)
        {
            if (status.Success)
                completed.Add(issue);
            else
                failed.Add(issue);
            inProgress.Remove(subagent);
        }
    }
    
    // Report progress every 30s
    if (TimeSinceLastReport > 30s)
        ReportProgress();
    
    await Task.Delay(PollInterval);
}
```

### 5. Progress Reporting

Periodically report to user:

```
## AI Factory Progress

**Queue Status:**
- Pending: 5 issues
- In Progress: 2 issues
- Completed: 3 issues
- Failed: 0 issues

**Currently Working:**
- Issue #123: Add user authentication (Subagent 1, 5m elapsed)
- Issue #124: Implement search feature (Subagent 2, 2m elapsed)

**Recently Completed:**
- ✓ Issue #120: Fix login bug → PR #456 (all checks green)
- ✓ Issue #121: Add logging → PR #457 (all checks green)
```

### 6. Completion

When queue is empty and all subagents finished:

```
## AI Factory Execution Complete

**Summary:**
- Total Issues: 8
- Completed: 7 (PRs ready for review)
- Failed: 1

**Completed PRs:**
- #456: Add user authentication (Issue #123)
- #457: Implement search feature (Issue #124)
...

**Failed Issues:**
- Issue #125: Complex refactoring - Error: Merge conflicts with master

**Next Steps:**
- Review and merge completed PRs
- Manually address failed issues
```

## Command Line Options

```bash
dotnet run -- [options]

Options:
  --max-concurrent <n>    Max concurrent subagents (default: 2)
  --org <name>            GitHub organization (default: auto-detect)
  --repo <name>           GitHub repository (default: auto-detect)
  --project-number <n>    Project number (default: auto-detect)
  --poll-interval <n>     Status check interval in seconds (default: 10)
  --dry-run               Show what would be done without executing
```

## Publishing

Create a single-file executable:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# macOS
dotnet publish -c Release -r osx-x64 --self-contained false -p:PublishSingleFile=true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true
```

## Error Handling

**Issue-level failures:**
- Merge conflicts → Report and skip
- Test failures → Report and skip
- Build errors → Report and skip
- Unclear requirements → Report and skip

**System-level failures:**
- GitHub API rate limit → Pause and retry with backoff
- Git authentication failure → Abort and report
- Subagent crash → Mark issue as failed and continue

**Recovery:**
- Failed issues remain in "Development" column
- User can re-run skill to retry failed issues
- Or manually move failed issues to "Blocked" column

## Guardrails

- **Never merge PRs automatically** - only monitor until green
- **Never force push** - respect git history
- **Never commit secrets** - validate before pushing
- **Respect rate limits** - implement exponential backoff
- **Isolate subagents** - one issue per subagent, no shared state
- **Fail fast** - if issue is ambiguous, report and skip
- **Preserve user control** - main agent remains responsive to user commands

## Integration with Existing Skills

This skill composes other skills:
- **checkin-dance** - Used by subagents for PR workflow
- **trufflehog** - Run before each commit
- **stylecop** - Run before each commit (if C# changes)
- **npm-audit** - Run if package.json changes

## Implementation Notes

**Single-file .NET 10 application:** The executor is implemented as a standard .NET console application with:
- `executor.cs` - Main implementation with all logic
- `executor.csproj` - Project file with dependencies
- Cross-platform execution (Windows, macOS, Linux)
- .NET 10 features (records, pattern matching, top-level statements)
- Minimal dependencies (System.CommandLine for argument parsing)
- Single-file publish support for easy distribution

**Dependencies:**
- `System.CommandLine` - Command-line argument parsing
- Standard .NET libraries for process management, JSON, and async operations

**Subagent Integration:** The current implementation includes placeholders for subagent spawning in the `SubagentManager` class. See [INTEGRATION.md](INTEGRATION.md) for integration patterns with Bob Shell's Task tool.

## Future Enhancements

- Support for issue dependencies (implement in order)
- Automatic PR review using AI
- Automatic merge when all checks pass (opt-in)
- Slack/Teams notifications for progress
- Metrics dashboard (issues/hour, success rate)
- Support for multiple projects simultaneously

## See Also

- [README.md](README.md) - Setup and usage guide
- [executor.cs](executor.cs) - Main C# implementation
- [executor.csproj](executor.csproj) - Project file
- [test-discovery.cs](test-discovery.cs) - Discovery test tool
- [INTEGRATION.md](INTEGRATION.md) - Subagent integration guide
- [EXAMPLE.md](EXAMPLE.md) - Usage examples
