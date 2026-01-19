using System.Diagnostics;
using System.Text;
using System.Text.Json;

var issueNumber = Environment.GetEnvironmentVariable("ISSUE_NUMBER")
    ?? throw new InvalidOperationException("ISSUE_NUMBER environment variable not set");
var repo = Environment.GetEnvironmentVariable("REPO")
    ?? throw new InvalidOperationException("REPO environment variable not set");

Console.WriteLine($"Processing issue #{issueNumber} in {repo}");

// Read the issue content using gh CLI
var issueJson = RunCommand("gh", $"issue view {issueNumber} --repo {repo} --json title,body,labels");
var issue = JsonDocument.Parse(issueJson);
var title = issue.RootElement.GetProperty("title").GetString() ?? "";
var body = issue.RootElement.GetProperty("body").GetString() ?? "";

Console.WriteLine($"Issue Title: {title}");
Console.WriteLine($"Issue Body Length: {body.Length} characters");

// Generate technical development tasks using Copilot CLI
var prompt = $"""
Analyze this GitHub issue and generate a list of specific technical development tasks.
Each task should be actionable and specific to this issue's requirements.
Return ONLY the tasks as a simple list, one task per line, no numbering or bullet points.

Issue Title: {title}

Issue Description:
{body}
""";

// Write prompt to temp file to handle special characters and multiline content
var promptFile = Path.GetTempFileName();
File.WriteAllText(promptFile, prompt);

string copilotResponse;
try
{
    // Use Copilot CLI with -p flag for prompt
    // Authentication uses COPILOT_GITHUB_TOKEN environment variable
    var promptContent = File.ReadAllText(promptFile);
    copilotResponse = RunCommand("copilot", $"-p \"{promptContent.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"");
}
finally
{
    File.Delete(promptFile);
}

// Parse the Copilot response into individual tasks
var technicalTasks = copilotResponse
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Select(line => line.Trim())
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .Select(line => line.TrimStart('-', '*', '•', ' ', '\t'))
    .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"^\d+[\.\)]\s*", ""))
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .ToList();

Console.WriteLine($"Generated {technicalTasks.Count} technical tasks");

// Build the updated issue body with technical tasks appended
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

// Update the issue body
var tempFile = Path.GetTempFileName();
File.WriteAllText(tempFile, updatedBody.ToString());

try
{
    RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --body-file \"{tempFile}\"");
    Console.WriteLine("Issue body updated with technical tasks");
}
finally
{
    File.Delete(tempFile);
}

// Remove the "3. Technical Design" label and add "4. Test Design" label
RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --remove-label \"3. Technical Design\"");
Console.WriteLine("Removed '3. Technical Design' label");

RunCommand("gh", $"issue edit {issueNumber} --repo {repo} --add-label \"4. Test Design\"");
Console.WriteLine("Added '4. Test Design' label");

Console.WriteLine($"Successfully processed issue #{issueNumber}");

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
