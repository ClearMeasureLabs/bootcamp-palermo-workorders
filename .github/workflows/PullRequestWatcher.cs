using System.Diagnostics;
using System.Text.Json;

// ============================================================================
// MAIN ENTRY POINT
// ============================================================================

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

var repo = args[0];
var issueNumber = args.Length > 1 ? args[1] : null;
var timings = new TimingMetrics();

LogStart(repo, issueNumber);

var currentUser = GetCurrentUser(timings);
var pullRequests = FindPullRequestsAwaitingReview(repo, currentUser, issueNumber, timings);

if (pullRequests.Count > 0)
{
    Log($"Found {pullRequests.Count} PR(s) awaiting review from {currentUser}");
    foreach (var pr in pullRequests)
    {
        Log($"  PR #{pr.Number}: {pr.Title}");
    }
}
else
{
    Log($"No PRs found awaiting review from {currentUser}");
}

LogComplete(currentUser, pullRequests.Count, timings);

return 0;

// ============================================================================
// PIPELINE STEPS
// ============================================================================

static void PrintUsage()
{
    Console.WriteLine("""
PullRequestWatcher - Watch for pull requests awaiting review

USAGE:
    dotnet run PullRequestWatcher.cs -- <repo> [issue-number]

ARGUMENTS:
    repo            Repository in format 'owner/repo'
    issue-number    Optional: Filter to PRs linked to this issue

EXAMPLES:
    dotnet run PullRequestWatcher.cs -- ClearMeasureLabs/bootcamp-workorders
    dotnet run PullRequestWatcher.cs -- ClearMeasureLabs/bootcamp-workorders 355

DESCRIPTION:
    Checks if the current authenticated user (PAT user) has been requested
    as a reviewer on any open pull requests. Optionally filters to PRs
    linked to a specific issue.

PREREQUISITES:
    - GitHub CLI (gh) authenticated with repo access
    - GH_TOKEN or GITHUB_TOKEN set with appropriate permissions

WORKFLOW:
    1. Get current authenticated user
    2. Query open PRs with review requests
    3. Filter to PRs where current user is requested reviewer
    4. Report findings
""");
}

static void LogStart(string repo, string? issueNumber)
{
    LogGroup("PullRequestWatcher Starting", () =>
    {
        Log($"Repository: {repo}");
        if (issueNumber != null)
            Log($"Filtering to Issue: {issueNumber}");
        Log($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    });
}

static string GetCurrentUser(TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    string user = "";

    LogGroup("Getting Current User", () =>
    {
        var result = RunCommand("gh", "api user --jq .login");
        user = result.Trim();
        Log($"Current PAT user: {user}");
    });

    sw.Stop();
    timings.GetUser = sw.Elapsed;
    Log($"Getting user took {sw.Elapsed.TotalSeconds:F2}s");

    return user;
}

static List<PullRequestInfo> FindPullRequestsAwaitingReview(string repo, string currentUser, string? issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    var results = new List<PullRequestInfo>();

    LogGroup("Finding PRs Awaiting Review", () =>
    {
        // Query open PRs with review requests
        var query = $"repo:{repo} is:pr is:open review-requested:{currentUser}";
        if (issueNumber != null)
        {
            // Also check if PR is linked to the issue
            query += $" linked:issue";
        }

        Log($"Searching with query: {query}");

        var searchResult = RunCommand("gh", $"pr list --repo {repo} --search \"review-requested:{currentUser}\" --json number,title,author,url,headRefName,body --limit 50");
        var prs = JsonDocument.Parse(searchResult);

        foreach (var pr in prs.RootElement.EnumerateArray())
        {
            var prNumber = pr.GetProperty("number").GetInt32();
            var prTitle = pr.GetProperty("title").GetString() ?? "";
            var prUrl = pr.GetProperty("url").GetString() ?? "";
            var prAuthor = pr.GetProperty("author").GetProperty("login").GetString() ?? "";
            var prBody = pr.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

            // If filtering by issue, check if PR references the issue
            if (issueNumber != null)
            {
                var issueRef = $"#{issueNumber}";
                var issueRefFull = $"issues/{issueNumber}";
                if (!prTitle.Contains(issueRef) && !prBody.Contains(issueRef) && !prBody.Contains(issueRefFull))
                {
                    Log($"  Skipping PR #{prNumber} - not linked to issue {issueNumber}");
                    continue;
                }
            }

            Log($"  Found PR #{prNumber}: {prTitle} (by {prAuthor})");
            results.Add(new PullRequestInfo
            {
                Number = prNumber,
                Title = prTitle,
                Url = prUrl,
                Author = prAuthor
            });
        }
    });

    sw.Stop();
    timings.FindPRs = sw.Elapsed;
    Log($"Finding PRs took {sw.Elapsed.TotalSeconds:F2}s");

    return results;
}

static void LogComplete(string currentUser, int prCount, TimingMetrics timings)
{
    var total = timings.GetUser + timings.FindPRs;

    LogGroup("PullRequestWatcher Complete", () =>
    {
        Log($"User: {currentUser}");
        Log($"PRs awaiting review: {prCount}");
        Log($"Timing Summary:");
        Log($"   - Get user:    {timings.GetUser.TotalSeconds,6:F2}s");
        Log($"   - Find PRs:    {timings.FindPRs.TotalSeconds,6:F2}s");
        Log($"   -------------------------------");
        Log($"   Total:         {total.TotalSeconds,6:F2}s");
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
    public TimeSpan GetUser { get; set; }
    public TimeSpan FindPRs { get; set; }
}

class PullRequestInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Author { get; set; } = "";
}
