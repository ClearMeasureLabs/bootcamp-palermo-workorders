using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

var (title, body) = ReadIssueContent(repo, issueNumber, timings);
var copilotResponse = GenerateTechnicalTasks(title, body, timings);
var tasks = ParseTasksFromResponse(copilotResponse, timings);
UpdateIssueBody(repo, issueNumber, body, tasks, timings);
TransitionLabels(repo, issueNumber, timings);

LogComplete(issueNumber, timings);

return 0;

// ============================================================================
// PIPELINE STEPS
// ============================================================================

static void PrintUsage()
{
    Console.WriteLine("""
IssueTasker - Generate technical development tasks from GitHub issues

USAGE:
    dotnet run IssueTasker.cs -- <repo> <issue-number>

ARGUMENTS:
    repo            Repository in format 'owner/repo'
    issue-number    The GitHub issue number to process

EXAMPLES:
    dotnet run IssueTasker.cs -- ClearMeasureLabs/bootcamp-workorders 42
    dotnet run IssueTasker.cs -- myorg/myrepo 123

DESCRIPTION:
    Reads a GitHub issue labeled '3. Technical Design', sends it to GitHub
    Copilot CLI to generate technical development tasks, updates the issue
    body with a checklist of tasks, and transitions the label to '4. Test Design'.

PREREQUISITES:
    - GitHub CLI (gh) authenticated with repo access
    - GitHub Copilot CLI (copilot) installed and authenticated
    - Issue must have '3. Technical Design' label

WORKFLOW:
    1. Read issue content from GitHub
    2. Load prompt template from IssueTasker-prompt.md
    3. Send to Copilot CLI for task generation
    4. Parse response into task list
    5. Update issue body with task checklist
    6. Transition label: '3. Technical Design' -> '4. Test Design'
""");
}

static void LogStart(string repo, string issueNumber)
{
    LogGroup("IssueTasker Starting", () =>
    {
        Log($"Repository: {repo}");
        Log($"Issue Number: {issueNumber}");
        Log($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    });
}

static (string title, string body) ReadIssueContent(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    var issueJson = RunCommand("gh", $"issue view {issueNumber} --repo {repo} --json title,body,labels");
    var issue = JsonDocument.Parse(issueJson);
    var title = issue.RootElement.GetProperty("title").GetString() ?? "";
    var body = issue.RootElement.GetProperty("body").GetString() ?? "";

    sw.Stop();
    timings.ReadIssue = sw.Elapsed;
    Log($"⏱️ Reading issue took {sw.Elapsed.TotalSeconds:F2}s");
    Log($"Issue Title: {title}");
    Log($"Issue Body Length: {body.Length} characters");

    return (title, body);
}

static string GenerateTechnicalTasks(string title, string body, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    string response = "";

    LogGroup("Generating Technical Tasks with Copilot", () =>
    {
        var promptTemplatePath = FindPromptTemplate();
        Log($"Loading prompt template from: {promptTemplatePath}");

        var promptTemplate = File.ReadAllText(promptTemplatePath);
        var prompt = promptTemplate
            .Replace("{title}", title)
            .Replace("{body}", body);

        Log("Sending prompt to Copilot CLI...");
        var escapedPrompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        response = RunCommand("copilot", $"-p \"{escapedPrompt}\"");
        Log($"Copilot response received ({response.Length} characters)");
    });

    sw.Stop();
    timings.CopilotGeneration = sw.Elapsed;
    Log($"⏱️ Copilot task generation took {sw.Elapsed.TotalSeconds:F2}s");

    return response;
}

static string FindPromptTemplate()
{
    var candidates = new[]
    {
        Path.Combine(Environment.CurrentDirectory, ".github", "workflows", "IssueTasker-prompt.md"),
        Path.Combine(Environment.CurrentDirectory, "IssueTasker-prompt.md"),
        "IssueTasker-prompt.md"
    };

    foreach (var path in candidates)
    {
        if (File.Exists(path)) return path;
    }

    throw new FileNotFoundException($"Could not find IssueTasker-prompt.md. Searched: {string.Join(", ", candidates)}");
}

static List<string> ParseTasksFromResponse(string copilotResponse, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    List<string> tasks = new();

    LogGroup("Parsing Copilot Response", () =>
    {
        tasks = copilotResponse
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.TrimStart('-', '*', '•', ' ', '\t'))
            .Select(line => Regex.Replace(line, @"^\d+[\.\)]\s*", ""))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        Log($"Parsed {tasks.Count} technical tasks:");
        for (int i = 0; i < tasks.Count; i++)
        {
            Log($"  {i + 1}. {tasks[i]}");
        }
    });

    sw.Stop();
    timings.ParseResponse = sw.Elapsed;
    Log($"⏱️ Parsing response took {sw.Elapsed.TotalSeconds:F2}s");

    return tasks;
}

static void UpdateIssueBody(string repo, string issueNumber, string originalBody, List<string> tasks, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Updating Issue Body", () =>
    {
        var updatedBody = BuildUpdatedBody(originalBody, tasks);
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, updatedBody);
            Log("Writing updated body to issue...");
            RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --body-file \"{tempFile}\"");
            Log("Issue body updated successfully");
        }
        finally
        {
            File.Delete(tempFile);
        }
    });

    sw.Stop();
    timings.UpdateIssue = sw.Elapsed;
    Log($"⏱️ Updating issue body took {sw.Elapsed.TotalSeconds:F2}s");
}

static string BuildUpdatedBody(string originalBody, List<string> tasks)
{
    var sb = new StringBuilder(originalBody);
    sb.AppendLine();
    sb.AppendLine();
    sb.AppendLine("---");
    sb.AppendLine();
    sb.AppendLine("## Technical Development Tasks");
    sb.AppendLine();

    foreach (var task in tasks)
    {
        sb.AppendLine($"- [ ] {task}");
    }

    sb.AppendLine();
    sb.AppendLine("---");

    return sb.ToString();
}

static void TransitionLabels(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Updating Labels", () =>
    {
        Log("Transitioning labels: '3. Technical Design' -> '4. Test Design'...");
        RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --remove-label \"3. Technical Design\" --add-label \"4. Test Design\"");
        Log("Labels updated successfully");
    });

    sw.Stop();
    timings.UpdateLabels = sw.Elapsed;
    Log($"⏱️ Updating labels took {sw.Elapsed.TotalSeconds:F2}s");
}

static void LogComplete(string issueNumber, TimingMetrics timings)
{
    var total = timings.ReadIssue + timings.CopilotGeneration + timings.ParseResponse + timings.UpdateIssue + timings.UpdateLabels;

    LogGroup("IssueTasker Complete", () =>
    {
        Log($"✅ Successfully processed issue #{issueNumber}");
        Log($"📊 Timing Summary:");
        Log($"   - Read issue:         {timings.ReadIssue.TotalSeconds,6:F2}s");
        Log($"   - Copilot generation: {timings.CopilotGeneration.TotalSeconds,6:F2}s");
        Log($"   - Parse response:     {timings.ParseResponse.TotalSeconds,6:F2}s");
        Log($"   - Update issue:       {timings.UpdateIssue.TotalSeconds,6:F2}s");
        Log($"   - Update labels:      {timings.UpdateLabels.TotalSeconds,6:F2}s");
        Log($"   ─────────────────────────────");
        Log($"   Total:                {total.TotalSeconds,6:F2}s");
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
    public TimeSpan CopilotGeneration { get; set; }
    public TimeSpan ParseResponse { get; set; }
    public TimeSpan UpdateIssue { get; set; }
    public TimeSpan UpdateLabels { get; set; }
}
