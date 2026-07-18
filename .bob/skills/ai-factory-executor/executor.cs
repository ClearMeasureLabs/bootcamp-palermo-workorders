// AI Factory Executor - Autonomous GitHub Issue Implementation
// Single-file .NET 10 application
// Build: dotnet build -c Release
// Run: dotnet run -- [options]
// Publish: dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// Main entry point - must come before type declarations
var rootCommand = new RootCommand("AI Factory Executor - Autonomous GitHub Issue Implementation");

var maxConcurrentOption = new Option<int>(
    "--max-concurrent",
    getDefaultValue: () => 2,
    description: "Maximum number of concurrent subagents");

var orgOption = new Option<string>(
    "--org",
    getDefaultValue: () => "",
    description: "GitHub organization (default: auto-detect)");

var repoOption = new Option<string>(
    "--repo",
    getDefaultValue: () => "",
    description: "GitHub repository (default: auto-detect)");

var projectNumberOption = new Option<int>(
    "--project-number",
    getDefaultValue: () => 0,
    description: "GitHub Projects v2 project number (default: auto-detect)");

var pollIntervalOption = new Option<int>(
    "--poll-interval",
    getDefaultValue: () => 10,
    description: "Status check interval in seconds");

var dryRunOption = new Option<bool>(
    "--dry-run",
    getDefaultValue: () => false,
    description: "Show what would be done without executing");

rootCommand.AddOption(maxConcurrentOption);
rootCommand.AddOption(orgOption);
rootCommand.AddOption(repoOption);
rootCommand.AddOption(projectNumberOption);
rootCommand.AddOption(pollIntervalOption);
rootCommand.AddOption(dryRunOption);

rootCommand.SetHandler(async (int maxConcurrent, string org, string repo, int projectNumber, int pollInterval, bool dryRun) =>
{
    try
    {
        Console.WriteLine("=== AI Factory Executor ===\n");
        
        var config = new Config
        {
            MaxConcurrent = maxConcurrent,
            Org = org,
            Repo = repo,
            ProjectNumber = projectNumber,
            PollInterval = pollInterval,
            DryRun = dryRun
        };
        
        if (!await GitHub.CheckPrerequisites())
            Environment.Exit(1);
        
        (config.Org, config.Repo) = await GitHub.GetRepoInfo(config.Org, config.Repo);
        
        var issues = await GitHub.GetEligibleIssues(config.Org, config.Repo);
        
        if (issues.Count == 0)
        {
            Console2.Info("No eligible issues to process");
            Environment.Exit(0);
        }
        
        if (config.DryRun)
        {
            Console2.Info($"[DRY RUN] Would process {issues.Count} issues:");
            foreach (var issue in issues)
            {
                Console.WriteLine($"  - Issue #{issue.Number}: {issue.Title}");
            }
            Environment.Exit(0);
        }
        
        await Orchestrator.Run(issues, config);
    }
    catch (Exception ex)
    {
        Console2.Failure($"Execution failed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        Environment.Exit(1);
    }
}, maxConcurrentOption, orgOption, repoOption, projectNumberOption, pollIntervalOption, dryRunOption);

return await rootCommand.InvokeAsync(args);

// Models
record Issue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("labels")] List<Label> Labels
);

record Label([property: JsonPropertyName("name")] string Name);

record PullRequest(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("state")] string State
);

record SubagentTask(
    Issue Issue,
    string Task,
    string BranchName,
    DateTime StartedAt
);

record Subagent(
    string Id,
    string Status,
    Issue Issue,
    DateTime StartedAt,
    Process? Process = null,
    string? OutputFile = null,
    string? ErrorFile = null
);

record SubagentStatus(
    bool Complete,
    bool Success,
    string? Error,
    string? Output = null
);

// Configuration
class Config
{
    public int MaxConcurrent { get; set; } = 2;
    public string Org { get; set; } = "";
    public string Repo { get; set; } = "";
    public int ProjectNumber { get; set; } = 0;
    public int PollInterval { get; set; } = 10;
    public bool DryRun { get; set; } = false;
}

// Console helpers
static class Console2
{
    public static void Success(string message) => 
        Console.WriteLine($"\u001b[32m✓ {message}\u001b[0m");
    
    public static void Failure(string message) => 
        Console.WriteLine($"\u001b[31m✗ {message}\u001b[0m");
    
    public static void Progress(string message) => 
        Console.WriteLine($"\u001b[36m→ {message}\u001b[0m");
    
    public static void Info(string message) => 
        Console.WriteLine($"\u001b[33mℹ {message}\u001b[0m");
}

// GitHub CLI wrapper
static class GitHub
{
    public static async Task<bool> CheckPrerequisites()
    {
        Console2.Progress("Checking prerequisites...");
        
        // Check gh CLI
        var ghCheck = await RunCommand("gh", "--version", captureOutput: true);
        if (ghCheck.ExitCode != 0)
        {
            Console2.Failure("GitHub CLI (gh) not found. Install: https://cli.github.com/");
            return false;
        }
        
        // Check gh auth
        var authCheck = await RunCommand("gh", "auth status", captureOutput: true);
        if (authCheck.ExitCode != 0)
        {
            Console2.Failure("GitHub CLI not authenticated. Run: gh auth login");
            return false;
        }
        
        Console2.Success("Prerequisites OK");
        return true;
    }
    
    public static async Task<(string Org, string Repo)> GetRepoInfo(string org, string repo)
    {
        if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(repo))
            return (org, repo);
        
        Console2.Progress("Auto-detecting repository from git remote...");
        
        var result = await RunCommand("git", "remote get-url origin", captureOutput: true);
        if (result.ExitCode != 0)
        {
            Console2.Failure("Could not detect org/repo. Specify with --org and --repo");
            Environment.Exit(1);
        }
        
        var match = Regex.Match(result.Output.Trim(), @"github\.com[:/]([^/]+)/([^/\.]+)");
        if (!match.Success)
        {
            Console2.Failure("Could not parse GitHub URL from git remote");
            Environment.Exit(1);
        }
        
        org = match.Groups[1].Value.Trim();
        repo = match.Groups[2].Value.Trim();
        Console2.Info($"Detected: {org}/{repo}");
        
        return (org, repo);
    }
    
    public static async Task<List<Issue>> GetEligibleIssues(string org, string repo)
    {
        Console2.Progress("Discovering issues with 'AI Factory' label in 'Development' column...");
        
        var result = await RunCommandWithArgs("gh", new[]
        {
            "issue", "list",
            "--repo", $"{org}/{repo}",
            "--label", "AI Factory",
            "--state", "open",
            "--json", "number,title,body,url,labels,state",
            "--limit", "100"
        }, captureOutput: true);
        
        if (result.ExitCode != 0)
        {
            Console2.Failure($"Failed to query issues: {result.Error}");
            return new List<Issue>();
        }
        
        var issues = JsonSerializer.Deserialize<List<Issue>>(result.Output) ?? new List<Issue>();
        
        if (issues.Count == 0)
        {
            Console2.Info("No issues found with 'AI Factory' label");
            return new List<Issue>();
        }
        
        // Filter out issues that already have PRs
        var eligibleIssues = new List<Issue>();
        foreach (var issue in issues)
        {
            var prResult = await RunCommandWithArgs("gh", new[]
            {
                "pr", "list",
                "--repo", $"{org}/{repo}",
                "--search", $"closes:#{issue.Number}",
                "--json", "number,state",
                "--limit", "1"
            }, captureOutput: true);
            
            if (prResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(prResult.Output))
            {
                var prs = JsonSerializer.Deserialize<List<PullRequest>>(prResult.Output) ?? new List<PullRequest>();
                
                if (prs.Count == 0)
                {
                    eligibleIssues.Add(issue);
                }
                else
                {
                    Console2.Info($"Skipping issue #{issue.Number} - already has PR #{prs[0].Number}");
                }
            }
            else
            {
                // If PR check fails, include the issue
                eligibleIssues.Add(issue);
            }
        }
        
        Console2.Success($"Found {eligibleIssues.Count} eligible issues");
        return eligibleIssues;
    }
    
    private static async Task<(int ExitCode, string Output, string Error)> RunCommand(
        string command, 
        string arguments, 
        bool captureOutput = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            return (-1, "", "Failed to start process");
        
        var output = captureOutput ? await process.StandardOutput.ReadToEndAsync() : "";
        var error = captureOutput ? await process.StandardError.ReadToEndAsync() : "";
        
        await process.WaitForExitAsync();
        
        return (process.ExitCode, output, error);
    }
    
    private static async Task<(int ExitCode, string Output, string Error)> RunCommandWithArgs(
        string command, 
        string[] arguments, 
        bool captureOutput = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }
        
        using var process = Process.Start(psi);
        if (process == null)
            return (-1, "", "Failed to start process");
        
        var output = captureOutput ? await process.StandardOutput.ReadToEndAsync() : "";
        var error = captureOutput ? await process.StandardError.ReadToEndAsync() : "";
        
        await process.WaitForExitAsync();
        
        return (process.ExitCode, output, error);
    }
}

// Subagent management
static class SubagentManager
{
    public static SubagentTask CreateTask(Issue issue)
    {
        var slug = Regex.Replace(issue.Title, @"[^a-zA-Z0-9]+", "-").ToLower().Trim('-');
        if (slug.Length > 50)
            slug = slug.Substring(0, 50).TrimEnd('-');
        var branchName = $"feature/issue-{issue.Number}-{slug}";
        
        var task = $@"Implement GitHub issue #{issue.Number}: {issue.Title}

**Issue URL:** {issue.Url}

**Description:**
{issue.Body}

**Instructions:**
1. Create feature branch: `{branchName}`
2. Read the issue description carefully - it contains the full requirements
3. Implement the changes following the issue's acceptance criteria
4. Run tests before committing
5. Commit with message: ""feat: {issue.Title} (#{issue.Number})""
6. Push branch and create PR with:
   - Title: {issue.Title}
   - Body: ""Closes #{issue.Number}\n\n{issue.Body}""
7. Monitor PR checks until all green
8. Report completion status

**Constraints:**
- Follow existing code patterns and architecture (see CLAUDE.md)
- Run trufflehog before pushing to check for secrets
- If C# changes, run stylecop
- If package.json changes, run npm audit
- If blocked or unclear, report back immediately
- Do not merge - only monitor until green
";
        
        return new SubagentTask(issue, task, branchName, DateTime.UtcNow);
    }
    
    public static Subagent StartSubagent(SubagentTask task, bool dryRun)
    {
        Console2.Progress($"Starting subagent for Issue #{task.Issue.Number}: {task.Issue.Title}");
        
        if (dryRun)
        {
            Console2.Info("[DRY RUN] Would spawn subagent with task:");
            Console.WriteLine(task.Task);
            return new Subagent(
                Id: $"subagent-{task.Issue.Number}",
                Status: "running",
                Issue: task.Issue,
                StartedAt: DateTime.UtcNow
            );
        }
        
        // Spawn bob CLI as subagent process via PowerShell
        var outputFile = Path.Combine(Path.GetTempPath(), $"subagent-{task.Issue.Number}-output.log");
        var errorFile = Path.Combine(Path.GetTempPath(), $"subagent-{task.Issue.Number}-error.log");
        var taskFile = Path.Combine(Path.GetTempPath(), $"subagent-{task.Issue.Number}-task.txt");
        
        // Write task to file to avoid escaping issues
        File.WriteAllText(taskFile, task.Task);
        
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"bob (Get-Content '{taskFile}' -Raw)\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."))
        };
        
        var process = Process.Start(psi);
        if (process == null)
        {
            Console2.Failure($"Failed to start subagent for issue #{task.Issue.Number}");
            return new Subagent(
                Id: $"subagent-{task.Issue.Number}",
                Status: "failed",
                Issue: task.Issue,
                StartedAt: DateTime.UtcNow
            );
        }
        
        // Redirect output to files in background
        _ = Task.Run(async () =>
        {
            try
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await File.WriteAllTextAsync(outputFile, output);
            }
            catch { }
        });
        
        _ = Task.Run(async () =>
        {
            try
            {
                var error = await process.StandardError.ReadToEndAsync();
                await File.WriteAllTextAsync(errorFile, error);
            }
            catch { }
        });
        
        Console2.Success($"Spawned subagent PID {process.Id} for issue #{task.Issue.Number}");
        
        return new Subagent(
            Id: $"subagent-{task.Issue.Number}",
            Status: "running",
            Issue: task.Issue,
            StartedAt: DateTime.UtcNow,
            Process: process,
            OutputFile: outputFile,
            ErrorFile: errorFile
        );
    }
    
    public static SubagentStatus GetStatus(Subagent subagent)
    {
        if (subagent.Process == null)
        {
            return new SubagentStatus(
                Complete: false,
                Success: false,
                Error: "No process associated with subagent"
            );
        }
        
        // Check if process has exited
        if (!subagent.Process.HasExited)
        {
            return new SubagentStatus(
                Complete: false,
                Success: false,
                Error: null
            );
        }
        
        // Process has completed
        var exitCode = subagent.Process.ExitCode;
        var success = exitCode == 0;
        
        string? output = null;
        string? error = null;
        
        // Read output file if it exists
        if (!string.IsNullOrEmpty(subagent.OutputFile) && File.Exists(subagent.OutputFile))
        {
            try
            {
                output = File.ReadAllText(subagent.OutputFile);
            }
            catch (Exception ex)
            {
                error = $"Failed to read output file: {ex.Message}";
            }
        }
        
        // Read error file if it exists
        if (!string.IsNullOrEmpty(subagent.ErrorFile) && File.Exists(subagent.ErrorFile))
        {
            try
            {
                var errorContent = File.ReadAllText(subagent.ErrorFile);
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    error = error == null ? errorContent : $"{error}\n{errorContent}";
                }
            }
            catch (Exception ex)
            {
                error = error == null ? $"Failed to read error file: {ex.Message}" : $"{error}\nFailed to read error file: {ex.Message}";
            }
        }
        
        if (!success && error == null)
        {
            error = $"Process exited with code {exitCode}";
        }
        
        return new SubagentStatus(
            Complete: true,
            Success: success,
            Error: error,
            Output: output
        );
    }
}

// Main orchestration
static class Orchestrator
{
    public static async Task Run(List<Issue> issues, Config config)
    {
        var pending = new Queue<Issue>(issues);
        var inProgress = new List<Subagent>();
        var completed = new List<Issue>();
        var failed = new List<(Issue Issue, string Error)>();
        
        var lastReport = DateTime.UtcNow;
        
        Console2.Info($"Starting orchestration with {pending.Count} issues (max {config.MaxConcurrent} concurrent)");
        
        while (pending.Count > 0 || inProgress.Count > 0)
        {
            // Start new work if slots available
            while (inProgress.Count < config.MaxConcurrent && pending.Count > 0)
            {
                var issue = pending.Dequeue();
                var task = SubagentManager.CreateTask(issue);
                var subagent = SubagentManager.StartSubagent(task, config.DryRun);
                inProgress.Add(subagent);
            }
            
            // Check subagent status
            var toRemove = new List<Subagent>();
            foreach (var subagent in inProgress)
            {
                var status = SubagentManager.GetStatus(subagent);
                
                if (status.Complete)
                {
                    if (status.Success)
                    {
                        completed.Add(subagent.Issue);
                        Console2.Success($"Issue #{subagent.Issue.Number} complete");
                    }
                    else
                    {
                        failed.Add((subagent.Issue, status.Error ?? "Unknown error"));
                        Console2.Failure($"Issue #{subagent.Issue.Number} failed: {status.Error}");
                    }
                    toRemove.Add(subagent);
                }
            }
            
            foreach (var subagent in toRemove)
            {
                inProgress.Remove(subagent);
            }
            
            // Report progress every 30s
            var now = DateTime.UtcNow;
            if ((now - lastReport).TotalSeconds > 30)
            {
                Console.WriteLine("\n## AI Factory Progress");
                Console.WriteLine($"Pending: {pending.Count} | In Progress: {inProgress.Count} | Completed: {completed.Count} | Failed: {failed.Count}");
                
                if (inProgress.Count > 0)
                {
                    Console.WriteLine("\nCurrently Working:");
                    foreach (var subagent in inProgress)
                    {
                        var elapsed = Math.Round((DateTime.UtcNow - subagent.StartedAt).TotalMinutes, 1);
                        Console.WriteLine($"  - Issue #{subagent.Issue.Number}: {subagent.Issue.Title} ({elapsed}m elapsed)");
                    }
                }
                Console.WriteLine();
                
                lastReport = now;
            }
            
            // Poll interval
            await Task.Delay(config.PollInterval * 1000);
        }
        
        // Final report
        Console.WriteLine("\n## AI Factory Execution Complete");
        Console.WriteLine("\nSummary:");
        Console.WriteLine($"  Total Issues: {issues.Count}");
        Console.WriteLine($"  Completed: {completed.Count}");
        Console.WriteLine($"  Failed: {failed.Count}");
        
        if (completed.Count > 0)
        {
            Console.WriteLine("\nCompleted Issues:");
            foreach (var issue in completed)
            {
                Console.WriteLine($"  ✓ Issue #{issue.Number}: {issue.Title}");
            }
        }
        
        if (failed.Count > 0)
        {
            Console.WriteLine("\nFailed Issues:");
            foreach (var (issue, error) in failed)
            {
                Console.WriteLine($"  ✗ Issue #{issue.Number}: {issue.Title}");
                Console.WriteLine($"    Error: {error}");
            }
        }
    }
}
