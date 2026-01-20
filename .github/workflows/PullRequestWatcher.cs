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

var currentUser = GetCurrentUser(timings);
var linkedPRs = FindPRsLinkedToIssue(repo, issueNumber, timings);
var prsAwaitingReview = CheckForPendingReviews(repo, linkedPRs, currentUser, timings);

if (prsAwaitingReview.Count > 0)
{
    Log($"Found {prsAwaitingReview.Count} PR(s) for issue #{issueNumber} awaiting review from {currentUser}");
    foreach (var pr in prsAwaitingReview)
    {
        Log($"  PR #{pr.Number}: {pr.Title}");
    }
    MarkPRsAsReady(repo, prsAwaitingReview, timings);
}
else
{
    Log($"No PRs for issue #{issueNumber} awaiting review from {currentUser}");
}

LogComplete(issueNumber, currentUser, linkedPRs.Count, prsAwaitingReview.Count, timings);

return 0;

// ============================================================================
// PIPELINE STEPS
// ============================================================================

static void PrintUsage()
{
    Console.WriteLine("""
PullRequestWatcher - Watch for pull requests awaiting review for a specific issue

USAGE:
    dotnet run PullRequestWatcher.cs -- <repo> <issue-number>

ARGUMENTS:
    repo            Repository in format 'owner/repo'
    issue-number    The issue number to find linked PRs for

EXAMPLES:
    dotnet run PullRequestWatcher.cs -- ClearMeasureLabs/bootcamp-workorders 355

DESCRIPTION:
    Finds pull requests linked to a specific issue and checks if the
    current authenticated user (PAT user) has a pending review request.
    If found, marks those PRs as ready for review.

PREREQUISITES:
    - GitHub CLI (gh) authenticated with repo access
    - GH_TOKEN or GITHUB_TOKEN set with appropriate permissions

WORKFLOW:
    1. Get current authenticated user
    2. Find PRs linked to the specified issue
    3. Check each PR for pending review requests for the PAT user
    4. Mark PRs with pending reviews as ready for review
    5. Report findings
""");
}

static void LogStart(string repo, string issueNumber)
{
    LogGroup("PullRequestWatcher Starting", () =>
    {
        Log($"Repository: {repo}");
        Log($"Issue Number: {issueNumber}");
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

static List<PullRequestInfo> FindPRsLinkedToIssue(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    var results = new List<PullRequestInfo>();

    LogGroup("Finding PRs Linked to Issue", () =>
    {
        var parts = repo.Split('/');
        var owner = parts[0];
        var repoName = parts[1];

        // Use GraphQL to find PRs that close/reference this issue
        Log($"Querying PRs linked to issue #{issueNumber}...");

        var query = $"query {{ repository(owner: \\\"{owner}\\\", name: \\\"{repoName}\\\") {{ issue(number: {issueNumber}) {{ timelineItems(itemTypes: [CONNECTED_EVENT, CROSS_REFERENCED_EVENT], first: 50) {{ nodes {{ ... on ConnectedEvent {{ source {{ ... on PullRequest {{ number title url state author {{ login }} }} }} }} ... on CrossReferencedEvent {{ source {{ ... on PullRequest {{ number title url state author {{ login }} }} }} }} }} }} }} }} }}";

        var result = RunCommand("gh", $"api graphql -f query=\"{query}\"");
        Log($"GraphQL response received");

        var doc = JsonDocument.Parse(result);
        var timelineItems = doc.RootElement
            .GetProperty("data")
            .GetProperty("repository")
            .GetProperty("issue")
            .GetProperty("timelineItems")
            .GetProperty("nodes");

        var seenPRs = new HashSet<int>();

        foreach (var item in timelineItems.EnumerateArray())
        {
            if (!item.TryGetProperty("source", out var source)) continue;
            if (!source.TryGetProperty("number", out var numberProp)) continue;

            var prNumber = numberProp.GetInt32();
            if (seenPRs.Contains(prNumber)) continue;
            seenPRs.Add(prNumber);

            var state = source.GetProperty("state").GetString() ?? "";
            if (state != "OPEN")
            {
                Log($"  Skipping PR #{prNumber} - state is {state}");
                continue;
            }

            var prTitle = source.GetProperty("title").GetString() ?? "";
            var prUrl = source.GetProperty("url").GetString() ?? "";
            var prAuthor = source.GetProperty("author").GetProperty("login").GetString() ?? "";

            Log($"  Found open PR #{prNumber}: {prTitle} (by {prAuthor})");
            results.Add(new PullRequestInfo
            {
                Number = prNumber,
                Title = prTitle,
                Url = prUrl,
                Author = prAuthor
            });
        }

        Log($"Found {results.Count} open PR(s) linked to issue #{issueNumber}");
    });

    sw.Stop();
    timings.FindLinkedPRs = sw.Elapsed;
    Log($"Finding linked PRs took {sw.Elapsed.TotalSeconds:F2}s");

    return results;
}

static List<PullRequestInfo> CheckForPendingReviews(string repo, List<PullRequestInfo> pullRequests, string currentUser, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    var results = new List<PullRequestInfo>();

    LogGroup("Checking for Pending Reviews", () =>
    {
        Log($"Checking {pullRequests.Count} PR(s) for review requests to {currentUser}...");

        foreach (var pr in pullRequests)
        {
            // Get review requests for this PR
            var reviewResult = RunCommand("gh", $"pr view {pr.Number} --repo {repo} --json reviewRequests");
            var reviewDoc = JsonDocument.Parse(reviewResult);
            var reviewRequests = reviewDoc.RootElement.GetProperty("reviewRequests");

            foreach (var request in reviewRequests.EnumerateArray())
            {
                string? requestedLogin = null;

                // Check if it's a user or team request
                if (request.TryGetProperty("login", out var loginProp))
                {
                    requestedLogin = loginProp.GetString();
                }
                else if (request.TryGetProperty("name", out var nameProp))
                {
                    // It's a team, skip for now
                    continue;
                }

                if (requestedLogin != null && requestedLogin.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"  PR #{pr.Number} has pending review request for {currentUser}");
                    results.Add(pr);
                    break;
                }
            }
        }

        Log($"Found {results.Count} PR(s) with pending review requests for {currentUser}");
    });

    sw.Stop();
    timings.CheckReviews = sw.Elapsed;
    Log($"Checking reviews took {sw.Elapsed.TotalSeconds:F2}s");

    return results;
}

static void MarkPRsAsReady(string repo, List<PullRequestInfo> pullRequests, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Marking PRs as Ready for Review", () =>
    {
        foreach (var pr in pullRequests)
        {
            Log($"Marking PR #{pr.Number} as ready for review...");
            try
            {
                RunCommand("gh", $"pr ready {pr.Number} --repo {repo}");
                Log($"  PR #{pr.Number} marked as ready for review");
            }
            catch (Exception ex)
            {
                Log($"  Warning: Could not mark PR #{pr.Number} as ready: {ex.Message}");
                Log($"  PR may already be ready or user lacks permission");
            }
        }
    });

    sw.Stop();
    timings.MarkReady = sw.Elapsed;
    Log($"Marking PRs as ready took {sw.Elapsed.TotalSeconds:F2}s");
}

static void LogComplete(string issueNumber, string currentUser, int linkedPRCount, int awaitingReviewCount, TimingMetrics timings)
{
    var total = timings.GetUser + timings.FindLinkedPRs + timings.CheckReviews + timings.MarkReady;

    LogGroup("PullRequestWatcher Complete", () =>
    {
        Log($"Issue: #{issueNumber}");
        Log($"User: {currentUser}");
        Log($"PRs linked to issue: {linkedPRCount}");
        Log($"PRs awaiting review: {awaitingReviewCount}");
        Log($"Timing Summary:");
        Log($"   - Get user:       {timings.GetUser.TotalSeconds,6:F2}s");
        Log($"   - Find linked PRs:{timings.FindLinkedPRs.TotalSeconds,6:F2}s");
        Log($"   - Check reviews:  {timings.CheckReviews.TotalSeconds,6:F2}s");
        Log($"   - Mark ready:     {timings.MarkReady.TotalSeconds,6:F2}s");
        Log($"   -------------------------------");
        Log($"   Total:            {total.TotalSeconds,6:F2}s");
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
    public TimeSpan FindLinkedPRs { get; set; }
    public TimeSpan CheckReviews { get; set; }
    public TimeSpan MarkReady { get; set; }
}

class PullRequestInfo
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Author { get; set; } = "";
}
