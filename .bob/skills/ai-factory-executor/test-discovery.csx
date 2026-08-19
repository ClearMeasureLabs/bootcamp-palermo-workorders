#!/usr/bin/env dotnet-script
#r "nuget: System.Text.Json, 9.0.0"

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ============================================================================
// AI Factory Discovery Test
// ============================================================================
// Validates GitHub API access and issue discovery without executing any
// implementations. Use this to verify the skill can find eligible issues
// before running the full executor.
//
// Usage:
//   dotnet-script test-discovery.csx
//   dotnet-script test-discovery.csx -- --org MyOrg --repo MyRepo
// ============================================================================

record Issue(int Number, string Title, string Body, string Url, string State);
record PullRequest(int Number, string State);

static class Console2
{
    public static void Success(string message) => 
        Console.WriteLine($"\u001b[32m✓ {message}\u001b[0m");
    
    public static void Failure(string message) => 
        Console.WriteLine($"\u001b[31m✗ {message}\u001b[0m");
    
    public static void Info(string message) => 
        Console.WriteLine($"\u001b[33mℹ {message}\u001b[0m");
}

static async Task<(int ExitCode, string Output, string Error)> RunCommand(
    string command, 
    string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = command,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    
    using var process = Process.Start(psi);
    if (process == null)
        return (-1, "", "Failed to start process");
    
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    
    await process.WaitForExitAsync();
    
    return (process.ExitCode, output, error);
}

// Parse command line args
var org = Args.Contains("--org") ? Args[Array.IndexOf(Args.ToArray(), "--org") + 1] : "";
var repo = Args.Contains("--repo") ? Args[Array.IndexOf(Args.ToArray(), "--repo") + 1] : "";

Console.WriteLine("=== AI Factory Discovery Test ===\n");

// Check gh CLI
Console.Write("Checking GitHub CLI...");
var ghCheck = await RunCommand("gh", "--version");
if (ghCheck.ExitCode != 0)
{
    Console.WriteLine();
    Console2.Failure("GitHub CLI (gh) not found");
    Console.WriteLine("Install from: https://cli.github.com/");
    return 1;
}
Console2.Success(" OK");

// Check auth
Console.Write("Checking GitHub authentication...");
var authCheck = await RunCommand("gh", "auth status");
if (authCheck.ExitCode != 0)
{
    Console.WriteLine();
    Console2.Failure("Not authenticated");
    Console.WriteLine("Run: gh auth login");
    return 1;
}
Console2.Success(" OK");

// Auto-detect repo
if (string.IsNullOrEmpty(org) || string.IsNullOrEmpty(repo))
{
    Console.Write("Auto-detecting repository...");
    var remoteResult = await RunCommand("git", "remote get-url origin");
    if (remoteResult.ExitCode != 0)
    {
        Console.WriteLine();
        Console2.Failure("Could not detect org/repo from git remote");
        Console.WriteLine("Specify with: --org <org> --repo <repo>");
        return 1;
    }
    
    var match = Regex.Match(remoteResult.Output, @"github\.com[:/]([^/]+)/([^/\.]+)");
    if (!match.Success)
    {
        Console.WriteLine();
        Console2.Failure("Could not parse GitHub URL");
        return 1;
    }
    
    org = match.Groups[1].Value;
    repo = match.Groups[2].Value;
    Console2.Success($" {org}/{repo}");
}

// Query issues
Console.Write("Querying issues with 'AI Factory' label...");
var issuesResult = await RunCommand("gh", 
    $"issue list --repo {org}/{repo} --label \"AI Factory\" --state open --json number,title,body,url,state --limit 100");

if (issuesResult.ExitCode != 0)
{
    Console.WriteLine();
    Console2.Failure($"Failed to query issues: {issuesResult.Error}");
    return 1;
}

var issues = JsonSerializer.Deserialize<List<Issue>>(issuesResult.Output) ?? new List<Issue>();
Console2.Success($" Found {issues.Count} issues");

if (issues.Count == 0)
{
    Console2.Info("No issues found with 'AI Factory' label");
    Console.WriteLine();
    Console.WriteLine("To test this skill, create an issue with:");
    Console.WriteLine("  1. Label: 'AI Factory'");
    Console.WriteLine("  2. Add to a GitHub Project");
    Console.WriteLine("  3. Move to 'Development' column");
    return 0;
}

// Display issues
Console.WriteLine();
Console.WriteLine("Issues with 'AI Factory' label:");
Console.WriteLine();

var readyCount = 0;
foreach (var issue in issues)
{
    // Check for existing PRs
    var prResult = await RunCommand("gh",
        $"pr list --repo {org}/{repo} --search \"closes:#{issue.Number}\" --json number,state --limit 1");
    
    var prs = JsonSerializer.Deserialize<List<PullRequest>>(prResult.Output) ?? new List<PullRequest>();
    
    var hasPR = prs.Count > 0;
    var status = hasPR ? $"HAS PR #{prs[0].Number}" : "READY";
    var color = hasPR ? "\u001b[90m" : "\u001b[32m"; // Gray or Green
    
    if (!hasPR) readyCount++;
    
    Console.WriteLine($"  Issue #{issue.Number}: {issue.Title}");
    Console.WriteLine($"    Status: {color}{status}\u001b[0m");
    Console.WriteLine($"    URL: \u001b[90m{issue.Url}\u001b[0m");
    
    if (!string.IsNullOrEmpty(issue.Body))
    {
        var preview = issue.Body.Substring(0, Math.Min(100, issue.Body.Length));
        if (issue.Body.Length > 100) preview += "...";
        Console.WriteLine($"    Preview: \u001b[90m{preview}\u001b[0m");
    }
    Console.WriteLine();
}

// Summary
Console.WriteLine("Summary:");
Console.WriteLine($"  Total issues with 'AI Factory' label: {issues.Count}");
Console.WriteLine($"  Ready to implement (no PR): {readyCount}");
Console.WriteLine($"  Already have PRs: {issues.Count - readyCount}");
Console.WriteLine();

if (readyCount > 0)
{
    Console2.Success($"Ready to run AI Factory Executor on {readyCount} issue(s)");
    Console.WriteLine();
    Console.WriteLine("Run: dotnet-script .bob/skills/ai-factory-executor/executor.csx");
}
else
{
    Console2.Info("All issues already have PRs - nothing to implement");
}

return 0;
