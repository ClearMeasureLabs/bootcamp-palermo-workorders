using System.Diagnostics;
using System.Text;
using System.Text.Json;

if (args.Length < 2)
{
    Console.WriteLine("Usage: IssueTasker <repo> <issue-number>");
    Console.WriteLine("  repo: Repository in format 'owner/repo'");
    Console.WriteLine("  issue-number: The GitHub issue number to process");
    return 1;
}

var repo = args[0];
var issueNumber = args[1];
var overallStopwatch = Stopwatch.StartNew();

LogGroup("IssueTasker Starting", () =>
{
    Log($"Repository: {repo}");
    Log($"Issue Number: {issueNumber}");
    Log($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
});

// Read the issue content
string title;
string body;
var readStopwatch = Stopwatch.StartNew();
LogGroup("Reading Issue Content", () =>
{
    var issueJson = RunCommand("gh", $"issue view {issueNumber} --repo {repo} --json title,body,labels");
    var issue = JsonDocument.Parse(issueJson);
    title = issue.RootElement.GetProperty("title").GetString() ?? "";
    body = issue.RootElement.GetProperty("body").GetString() ?? "";
    Log($"Issue Title: {title}");
    Log($"Issue Body Length: {body.Length} characters");
});
readStopwatch.Stop();
Log($"⏱️ Reading issue took {readStopwatch.Elapsed.TotalSeconds:F2}s");

// Need to re-read for scope (C# limitation with lambdas)
var issueJson2 = RunCommand("gh", $"issue view {issueNumber} --repo {repo} --json title,body,labels");
var issue2 = JsonDocument.Parse(issueJson2);
title = issue2.RootElement.GetProperty("title").GetString() ?? "";
body = issue2.RootElement.GetProperty("body").GetString() ?? "";

// Generate technical development tasks using Copilot CLI
string copilotResponse = "";
var copilotStopwatch = Stopwatch.StartNew();
LogGroup("Generating Technical Tasks with Copilot", () =>
{
    var prompt = $"""
Analyze this GitHub issue and generate a list of specific technical development tasks.
Each task should be actionable and specific to this issue's requirements.
Return ONLY the tasks as a simple list, one task per line, no numbering or bullet points.

Issue Title: {title}

Issue Description:
{body}
""";

    Log("Sending prompt to Copilot CLI...");
    var promptFile = Path.GetTempFileName();
    File.WriteAllText(promptFile, prompt);

    try
    {
        var promptContent = File.ReadAllText(promptFile);
        copilotResponse = RunCommand("copilot", $"-p \"{promptContent.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"");
        Log($"Copilot response received ({copilotResponse.Length} characters)");
    }
    finally
    {
        File.Delete(promptFile);
    }
});
copilotStopwatch.Stop();
Log($"⏱️ Copilot task generation took {copilotStopwatch.Elapsed.TotalSeconds:F2}s");

// Parse the response into tasks
List<string> technicalTasks = new();
var parseStopwatch = Stopwatch.StartNew();
LogGroup("Parsing Copilot Response", () =>
{
    technicalTasks = copilotResponse
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim())
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(line => line.TrimStart('-', '*', '•', ' ', '\t'))
        .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"^\d+[\.\)]\s*", ""))
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToList();

    Log($"Parsed {technicalTasks.Count} technical tasks:");
    for (int i = 0; i < technicalTasks.Count; i++)
    {
        Log($"  {i + 1}. {technicalTasks[i]}");
    }
});
parseStopwatch.Stop();
Log($"⏱️ Parsing response took {parseStopwatch.Elapsed.TotalSeconds:F2}s");

// Build and update the issue body
var updateStopwatch = Stopwatch.StartNew();
LogGroup("Updating Issue Body", () =>
{
    var updatedBody = new StringBuilder(body);
    updatedBody.AppendLine();
    updatedBody.AppendLine();
    updatedBody.AppendLine("---");
    updatedBody.AppendLine();
    updatedBody.AppendLine("## Technical Development Tasks");
    updatedBody.AppendLine();
    updatedBody.AppendLine("_Generated automatically by GitHub Copilot from technical design analysis_");
    updatedBody.AppendLine();

    foreach (var task in technicalTasks)
    {
        updatedBody.AppendLine($"- [ ] {task}");
    }

    updatedBody.AppendLine();
    updatedBody.AppendLine("---");

    var tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, updatedBody.ToString());

    try
    {
        Log("Writing updated body to issue...");
        RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --body-file \"{tempFile}\"");
        Log("Issue body updated successfully");
    }
    finally
    {
        File.Delete(tempFile);
    }
});
updateStopwatch.Stop();
Log($"⏱️ Updating issue body took {updateStopwatch.Elapsed.TotalSeconds:F2}s");

// Update labels
var labelStopwatch = Stopwatch.StartNew();
LogGroup("Updating Labels", () =>
{
    Log("Removing '3. Technical Design' label...");
    RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --remove-label \"3. Technical Design\"");
    Log("Removed '3. Technical Design' label");

    Log("Adding '4. Test Design' label...");
    RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --add-label \"4. Test Design\"");
    Log("Added '4. Test Design' label");
});
labelStopwatch.Stop();
Log($"⏱️ Updating labels took {labelStopwatch.Elapsed.TotalSeconds:F2}s");

overallStopwatch.Stop();
LogGroup("IssueTasker Complete", () =>
{
    Log($"✅ Successfully processed issue #{issueNumber}");
    Log($"📊 Timing Summary:");
    Log($"   - Read issue:      {readStopwatch.Elapsed.TotalSeconds,6:F2}s");
    Log($"   - Copilot generation: {copilotStopwatch.Elapsed.TotalSeconds,6:F2}s");
    Log($"   - Parse response:  {parseStopwatch.Elapsed.TotalSeconds,6:F2}s");
    Log($"   - Update issue:    {updateStopwatch.Elapsed.TotalSeconds,6:F2}s");
    Log($"   - Update labels:   {labelStopwatch.Elapsed.TotalSeconds,6:F2}s");
    Log($"   ─────────────────────────");
    Log($"   Total:             {overallStopwatch.Elapsed.TotalSeconds,6:F2}s");
});

return 0;

// Helper methods
static void Log(string message)
{
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
}

static void LogGroup(string groupName, Action action)
{
    Console.WriteLine($"::group::{groupName}");
    var sw = Stopwatch.StartNew();
    try
    {
        action();
    }
    finally
    {
        sw.Stop();
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
