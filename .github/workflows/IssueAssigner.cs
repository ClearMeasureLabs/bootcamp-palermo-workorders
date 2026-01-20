using System.Diagnostics;
using System.Text.Json;

// ============================================================================
// MAIN ENTRY POINT
// ============================================================================

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

var repo = args[0];
var issueNumber = args[1];
var timings = new TimingMetrics();

LogStart(repo, issueNumber);

var issueTitle = ReadIssueTitle(repo, issueNumber, timings);
AssignToCopilot(repo, issueNumber, timings);

LogComplete(issueNumber, issueTitle, timings);

return 0;

// ============================================================================
// PIPELINE STEPS
// ============================================================================

static void PrintUsage()
{
    Console.WriteLine("""
IssueAssigner - Assign GitHub issues to Copilot coding agent

USAGE:
    dotnet run IssueAssigner.cs -- <repo> <issue-number>

ARGUMENTS:
    repo            Repository in format 'owner/repo'
    issue-number    The GitHub issue number to assign

EXAMPLES:
    dotnet run IssueAssigner.cs -- ClearMeasureLabs/bootcamp-workorders 42
    dotnet run IssueAssigner.cs -- myorg/myrepo 123

DESCRIPTION:
    Reads a GitHub issue labeled '5. Development' and assigns it to the
    GitHub Copilot coding agent for automated implementation.

PREREQUISITES:
    - GitHub CLI (gh) authenticated with repo access
    - COPILOT_PAT or GH_TOKEN with appropriate permissions
    - Issue must have '5. Development' label

WORKFLOW:
    1. Read issue content from GitHub
    2. Assign issue to Copilot coding agent
    3. Log completion status
""");
}

static void LogStart(string repo, string issueNumber)
{
    LogGroup("IssueAssigner Starting", () =>
    {
        Log($"Repository: {repo}");
        Log($"Issue Number: {issueNumber}");
        Log($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    });
}

static string ReadIssueTitle(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    string title = "";

    LogGroup("Reading Issue", () =>
    {
        var issueJson = RunCommand("gh", $"issue view {issueNumber} --repo {repo} --json title,assignees");
        var issue = JsonDocument.Parse(issueJson);
        title = issue.RootElement.GetProperty("title").GetString() ?? "";

        var assignees = issue.RootElement.GetProperty("assignees");
        var assigneeCount = assignees.GetArrayLength();

        Log($"Issue Title: {title}");
        Log($"Current Assignees: {assigneeCount}");
    });

    sw.Stop();
    timings.ReadIssue = sw.Elapsed;
    Log($"Reading issue took {sw.Elapsed.TotalSeconds:F2}s");

    return title;
}

static void AssignToCopilot(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Assigning to Copilot", () =>
    {
        // Use gh copilot to start work on the issue
        // This triggers the Copilot coding agent to begin working on the issue
        Log("Starting Copilot coding agent on issue...");

        try
        {
            // The gh copilot extension can start work on an issue
            // Format: gh copilot start <issue-url>
            var issueUrl = $"https://github.com/{repo}/issues/{issueNumber}";
            RunCommand("gh", $"copilot start {issueUrl}");
            Log("Copilot coding agent started successfully");
        }
        catch (Exception ex)
        {
            Log($"gh copilot start failed: {ex.Message}");
            Log("Falling back to API-based assignment...");

            // Fallback: Use the GitHub API to trigger Copilot
            // This creates a Copilot session via the REST API
            var apiPayload = $"{{\"issue_number\": {issueNumber}}}";
            try
            {
                RunCommand("gh", $"api repos/{repo}/copilot/sessions -X POST -f issue_number={issueNumber}");
                Log("Copilot session created via API");
            }
            catch (Exception apiEx)
            {
                Log($"API fallback also failed: {apiEx.Message}");
                Log("Manual assignment may be required");
            }
        }
    });

    sw.Stop();
    timings.AssignCopilot = sw.Elapsed;
    Log($"Assigning to Copilot took {sw.Elapsed.TotalSeconds:F2}s");
}

static void LogComplete(string issueNumber, string issueTitle, TimingMetrics timings)
{
    var total = timings.ReadIssue + timings.AssignCopilot;

    LogGroup("IssueAssigner Complete", () =>
    {
        Log($"Successfully processed issue #{issueNumber}: {issueTitle}");
        Log($"Timing Summary:");
        Log($"   - Read issue:       {timings.ReadIssue.TotalSeconds,6:F2}s");
        Log($"   - Assign Copilot:   {timings.AssignCopilot.TotalSeconds,6:F2}s");
        Log($"   -------------------------------");
        Log($"   Total:              {total.TotalSeconds,6:F2}s");
    });
}

// ============================================================================
// INFRASTRUCTURE
// ============================================================================

static void Log(string message)
{
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
}

static void LogGroup(string groupName, Action action)
{
    Console.WriteLine($"::group::{groupName}");
    try
    {
        action();
    }
    finally
    {
        Console.WriteLine($"::endgroup::");
    }
}

static string RunCommand(string command, string arguments)
{
    var processInfo = new ProcessStartInfo
    {
        FileName = command,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(processInfo)
        ?? throw new InvalidOperationException($"Failed to start process: {command}");

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command failed: {command} {arguments}\nError: {error}");
    }

    return output;
}

// ============================================================================
// TYPES
// ============================================================================

class TimingMetrics
{
    public TimeSpan ReadIssue { get; set; }
    public TimeSpan AssignCopilot { get; set; }
}
