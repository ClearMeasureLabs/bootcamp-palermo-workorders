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
var copilotResponse = GenerateTestDesign(title, body, timings);
var testDesign = ParseTestDesign(copilotResponse, timings);
UpdateIssueBody(repo, issueNumber, body, testDesign, timings);
TransitionLabels(repo, issueNumber, timings);

LogComplete(issueNumber, timings);

return 0;

// ============================================================================
// PIPELINE STEPS
// ============================================================================

static void PrintUsage()
{
    Console.WriteLine("""
AcceptanceTestDesigner - Generate acceptance test specifications from GitHub issues

USAGE:
    dotnet run AcceptanceTestDesigner.cs -- <repo> <issue-number>

ARGUMENTS:
    repo            Repository in format 'owner/repo'
    issue-number    The GitHub issue number to process

EXAMPLES:
    dotnet run AcceptanceTestDesigner.cs -- ClearMeasureLabs/bootcamp-workorders 42
    dotnet run AcceptanceTestDesigner.cs -- myorg/myrepo 123

DESCRIPTION:
    Reads a GitHub issue labeled '4. Test Design', sends it to GitHub
    Copilot CLI to generate acceptance test specifications, updates the issue
    body with test designs, and transitions the label to '5. Development'.

PREREQUISITES:
    - GitHub CLI (gh) authenticated with repo access
    - GitHub Copilot CLI (copilot) installed and authenticated
    - Issue must have '4. Test Design' label

WORKFLOW:
    1. Read issue content from GitHub
    2. Load prompt template from AcceptanceTestDesigner-prompt.md
    3. Send to Copilot CLI for test design generation
    4. Parse response into test specifications
    5. Update issue body with test design section
    6. Transition label: '4. Test Design' -> '5. Development'
""");
}

static void LogStart(string repo, string issueNumber)
{
    LogGroup("AcceptanceTestDesigner Starting", () =>
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
    Log($"Issue Title: {title}");
    Log($"Issue Body Length: {body.Length} characters");
    Log($"Reading issue took {sw.Elapsed.TotalSeconds:F2}s");

    return (title, body);
}

static string GenerateTestDesign(string title, string body, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    string response = "";

    LogGroup("Generating Test Design with Copilot", () =>
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
    Log($"Copilot test design generation took {sw.Elapsed.TotalSeconds:F2}s");

    return response;
}

static string FindPromptTemplate()
{
    var candidates = new[]
    {
        Path.Combine(Environment.CurrentDirectory, ".github", "workflows", "AcceptanceTestDesigner-prompt.md"),
        Path.Combine(Environment.CurrentDirectory, "AcceptanceTestDesigner-prompt.md"),
        "AcceptanceTestDesigner-prompt.md"
    };

    foreach (var path in candidates)
    {
        if (File.Exists(path)) return path;
    }

    throw new FileNotFoundException($"Could not find AcceptanceTestDesigner-prompt.md. Searched: {string.Join(", ", candidates)}");
}

static List<TestSpecification> ParseTestDesign(string copilotResponse, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();
    var tests = new List<TestSpecification>();

    LogGroup("Parsing Copilot Response", () =>
    {
        Log("Raw Copilot response:");
        Log(copilotResponse);
        Log("--- End of response ---");

        var lines = copilotResponse.Split('\n');
        TestSpecification? current = null;
        bool inSteps = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Match TEST: or **TEST:** or ### TEST: patterns
            if (line.StartsWith("TEST:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("**TEST:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TEST:"))
            {
                if (current != null && !string.IsNullOrWhiteSpace(current.Name)) tests.Add(current);
                var testName = ExtractValue(line, "TEST:");
                current = new TestSpecification { Name = testName };
                inSteps = false;
            }
            // Match FIXTURE: or **FIXTURE:** patterns
            else if ((line.StartsWith("FIXTURE:", StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("**FIXTURE:", StringComparison.OrdinalIgnoreCase) ||
                      line.Contains("FIXTURE:")) && current != null)
            {
                current.Fixture = ExtractValue(line, "FIXTURE:");
                inSteps = false;
            }
            // Match STEPS: header
            else if (line.StartsWith("STEPS:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("**STEPS:", StringComparison.OrdinalIgnoreCase))
            {
                inSteps = true;
            }
            // Collect step items
            else if (inSteps && current != null &&
                     (line.StartsWith("-") || line.StartsWith("*") || line.StartsWith("•") ||
                      Regex.IsMatch(line, @"^\d+\.")))
            {
                var step = Regex.Replace(line, @"^[-*•\d.]+\s*", "").Trim();
                if (!string.IsNullOrWhiteSpace(step))
                {
                    current.Steps.Add(step);
                }
            }
        }

        if (current != null && !string.IsNullOrWhiteSpace(current.Name)) tests.Add(current);

        Log($"Parsed {tests.Count} test specifications:");
        foreach (var test in tests)
        {
            Log($"  - {test.Name} ({test.Fixture}) - {test.Steps.Count} steps");
        }
    });

    sw.Stop();
    timings.ParseResponse = sw.Elapsed;
    Log($"Parsing response took {sw.Elapsed.TotalSeconds:F2}s");

    return tests;
}

static string ExtractValue(string line, string key)
{
    var idx = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
    if (idx >= 0)
    {
        var value = line.Substring(idx + key.Length).Trim();
        // Remove markdown formatting like ** or `
        value = value.Trim('*', '`', ' ');
        return value;
    }
    return line;
}

static void UpdateIssueBody(string repo, string issueNumber, string originalBody, List<TestSpecification> tests, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Updating Issue Body", () =>
    {
        var updatedBody = BuildUpdatedBody(originalBody, tests);
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
    Log($"Updating issue body took {sw.Elapsed.TotalSeconds:F2}s");
}

static string BuildUpdatedBody(string originalBody, List<TestSpecification> tests)
{
    var sb = new StringBuilder(originalBody);
    sb.AppendLine();
    sb.AppendLine();
    sb.AppendLine("---");
    sb.AppendLine();
    sb.AppendLine("## Acceptance Test Design");
    sb.AppendLine();

    foreach (var test in tests)
    {
        sb.AppendLine($"### `{test.Name}`");
        sb.AppendLine($"**Fixture:** `{test.Fixture}`");
        sb.AppendLine();
        sb.AppendLine("**Steps:**");
        foreach (var step in test.Steps)
        {
            sb.AppendLine($"- {step}");
        }
        sb.AppendLine();
    }

    sb.AppendLine("---");

    return sb.ToString();
}

static void TransitionLabels(string repo, string issueNumber, TimingMetrics timings)
{
    var sw = Stopwatch.StartNew();

    LogGroup("Updating Labels", () =>
    {
        Log("Transitioning labels: '4. Test Design' -> '5. Development'...");
        RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --remove-label \"4. Test Design\" --add-label \"5. Development\"");
        Log("Labels updated successfully");
    });

    sw.Stop();
    timings.UpdateLabels = sw.Elapsed;
    Log($"Updating labels took {sw.Elapsed.TotalSeconds:F2}s");
}

static void LogComplete(string issueNumber, TimingMetrics timings)
{
    var total = timings.ReadIssue + timings.CopilotGeneration + timings.ParseResponse + timings.UpdateIssue + timings.UpdateLabels;

    LogGroup("AcceptanceTestDesigner Complete", () =>
    {
        Log($"Successfully processed issue #{issueNumber}");
        Log($"Timing Summary:");
        Log($"   - Read issue:         {timings.ReadIssue.TotalSeconds,6:F2}s");
        Log($"   - Copilot generation: {timings.CopilotGeneration.TotalSeconds,6:F2}s");
        Log($"   - Parse response:     {timings.ParseResponse.TotalSeconds,6:F2}s");
        Log($"   - Update issue:       {timings.UpdateIssue.TotalSeconds,6:F2}s");
        Log($"   - Update labels:      {timings.UpdateLabels.TotalSeconds,6:F2}s");
        Log($"   -------------------------------");
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

class TestSpecification
{
    public string Name { get; set; } = "";
    public string Fixture { get; set; } = "";
    public List<string> Steps { get; set; } = new();
}
