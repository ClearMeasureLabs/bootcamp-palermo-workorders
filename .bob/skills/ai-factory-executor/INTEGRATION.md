# Integration Guide: AI Factory Executor with Bob Shell Task Tool

This document describes how to integrate the AI Factory Executor with Bob Shell's Task tool for true subagent delegation.

## Current State

The executor script (`executor.ps1`) includes placeholder functions for subagent management:
- `Start-Subagent` - Spawns a subagent with an issue implementation task
- `Get-SubagentStatus` - Checks subagent completion status

These need to be replaced with actual Task tool invocations.

## Integration Pattern

### Option 1: Bob Shell Task Tool (Preferred)

When Bob Shell's Task tool is available, replace the placeholder functions:

```powershell
function Start-Subagent {
    param($SubagentTask)
    
    Write-Progress "Starting subagent for Issue #$($SubagentTask.Issue.number)"
    
    # Invoke Bob Shell Task tool
    $taskId = bob task create `
        --name "issue-$($SubagentTask.Issue.number)" `
        --description "Implement GitHub issue #$($SubagentTask.Issue.number)" `
        --prompt $SubagentTask.Task `
        --isolated `
        --timeout 3600
    
    return @{
        Id = $taskId
        Status = "running"
        Issue = $SubagentTask.Issue
        StartedAt = Get-Date
    }
}

function Get-SubagentStatus {
    param($Subagent)
    
    # Query task status
    $status = bob task status --id $Subagent.Id --json | ConvertFrom-Json
    
    return @{
        Complete = ($status.state -eq "completed" -or $status.state -eq "failed")
        Success = ($status.state -eq "completed")
        Error = $status.error
        Output = $status.output
    }
}
```

### Option 2: Direct Subprocess (Fallback)

If Task tool is not available, spawn Bob Shell as a subprocess:

```powershell
function Start-Subagent {
    param($SubagentTask)
    
    # Write task to temp file
    $taskFile = [System.IO.Path]::GetTempFileName()
    $SubagentTask.Task | Out-File -FilePath $taskFile -Encoding UTF8
    
    # Start Bob Shell in background
    $process = Start-Process -FilePath "bob" `
        -ArgumentList "execute", "--file", $taskFile, "--json" `
        -NoNewWindow `
        -PassThru `
        -RedirectStandardOutput "$taskFile.out" `
        -RedirectStandardError "$taskFile.err"
    
    return @{
        Id = $process.Id
        Process = $process
        Issue = $SubagentTask.Issue
        OutputFile = "$taskFile.out"
        ErrorFile = "$taskFile.err"
        StartedAt = Get-Date
    }
}

function Get-SubagentStatus {
    param($Subagent)
    
    $process = $Subagent.Process
    
    if ($process.HasExited) {
        $output = Get-Content $Subagent.OutputFile -Raw -ErrorAction SilentlyContinue
        $error = Get-Content $Subagent.ErrorFile -Raw -ErrorAction SilentlyContinue
        
        return @{
            Complete = $true
            Success = ($process.ExitCode -eq 0)
            Error = if ($process.ExitCode -ne 0) { $error } else { $null }
            Output = $output
        }
    }
    
    return @{
        Complete = $false
        Success = $false
        Error = $null
    }
}
```

### Option 3: MCP Server Integration

If using Model Context Protocol servers:

```powershell
function Start-Subagent {
    param($SubagentTask)
    
    # Connect to MCP server
    $mcpClient = New-McpClient -ServerUrl "http://localhost:3000"
    
    # Create task via MCP
    $taskId = $mcpClient.CreateTask(@{
        type = "github-issue-implementation"
        issueNumber = $SubagentTask.Issue.number
        issueTitle = $SubagentTask.Issue.title
        issueBody = $SubagentTask.Issue.body
        branchName = $SubagentTask.BranchName
        instructions = $SubagentTask.Task
    })
    
    return @{
        Id = $taskId
        Client = $mcpClient
        Issue = $SubagentTask.Issue
        StartedAt = Get-Date
    }
}

function Get-SubagentStatus {
    param($Subagent)
    
    $status = $Subagent.Client.GetTaskStatus($Subagent.Id)
    
    return @{
        Complete = ($status.state -in @("completed", "failed"))
        Success = ($status.state -eq "completed")
        Error = $status.error
        Output = $status.result
    }
}
```

## Subagent Task Structure

The task passed to subagents should include:

```
Implement GitHub issue #<number>: <title>

**Issue URL:** <url>
**Description:** <body>

**Instructions:**
1. Create feature branch: `feature/issue-<number>-<slug>`
2. Read the issue description carefully
3. Implement the changes following acceptance criteria
4. Run quality checks:
   - Tests: `pwsh PrivateBuild.ps1`
   - Secrets: `pwsh .claude/skills/trufflehog/scripts/scan_changes.sh`
   - Style (if C#): `pwsh .claude/skills/stylecop/scripts/analyze.ps1`
   - Security (if npm): `npm audit`
5. Commit: "feat: <title> (#<number>)"
6. Push and create PR
7. Monitor PR checks until green
8. Report completion

**Constraints:**
- Follow CLAUDE.md architecture guidelines
- Never commit secrets
- Never force push
- Do not merge - only monitor until green
```

## Expected Subagent Output

Subagents should return structured output:

```json
{
  "status": "success|failed",
  "issue_number": 123,
  "branch_name": "feature/issue-123-add-auth",
  "pr_number": 456,
  "pr_url": "https://github.com/org/repo/pull/456",
  "checks_status": "passed|failed",
  "error": null,
  "duration_seconds": 180
}
```

## Error Handling

Subagents should handle and report:
- Merge conflicts
- Test failures
- Build errors
- Unclear requirements
- Timeout (default: 1 hour per issue)

## Testing Integration

Test with a single issue first:

```powershell
# Test discovery
pwsh .bob/skills/ai-factory-executor/test-discovery.ps1

# Test single issue (manual)
$issue = @{
    number = 123
    title = "Test issue"
    body = "Test implementation"
    url = "https://github.com/org/repo/issues/123"
}

$task = New-SubagentTask -Issue $issue
$subagent = Start-Subagent -SubagentTask $task

# Poll status
while (-not (Get-SubagentStatus -Subagent $subagent).Complete) {
    Start-Sleep -Seconds 5
}

$finalStatus = Get-SubagentStatus -Subagent $subagent
Write-Host "Status: $($finalStatus.Success)"
```

## Next Steps

1. Implement Task tool integration in Bob Shell
2. Update `Start-Subagent` and `Get-SubagentStatus` in `executor.ps1`
3. Test with single issue
4. Test with multiple concurrent issues
5. Add retry logic for transient failures
6. Add metrics collection (issues/hour, success rate)
7. Add notification integration (Slack, Teams)

## See Also

- [SKILL.md](SKILL.md) - Skill specification
- [README.md](README.md) - Usage guide
- [executor.ps1](executor.ps1) - Main implementation
