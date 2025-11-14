using System.Diagnostics;
using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using DbUp.Support;
using Microsoft.Data.SqlClient;

int Fail(string message, int code = -1)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
    return code;
}


#region Read command line parameters

// Takes 4 arguments, just like AliaASql
// <ALIASQL ACTION> <SERVER NAME> <DATABASE NAME> <SCRIPT DIRECTORY>
// Check for 4 command line parameters
if (args.Length < 4 ||
    string.IsNullOrWhiteSpace(args[0]) ||
    string.IsNullOrWhiteSpace(args[1]) ||
    string.IsNullOrWhiteSpace(args[2]) ||
    string.IsNullOrWhiteSpace(args[3]))
{
    return Fail("Exactly 4 parameters required: <action> <server> <database> <scriptDir>");
}

// [TO20251111] Ignore the <ALIASQL ACTION>
var serverName = args[1];
var databaseName = args[2];
// Normalize path separators to be platform-appropriate
var scriptDir = args[3].Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

#endregion

#region Check for Docker

// Check if Docker is running and container exists
int CheckDockerContainer(string containerName)
{
    try
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "ps --format \"{{.Names}}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            return Fail("Failed to start docker process. Is Docker installed?");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return Fail($"Docker is not accessible. Error: {error}");
        }

        var containerNames = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!containerNames.Contains(containerName))
        {
            return Fail(
                $"Docker container '{containerName}' is not running. Available containers: {string.Join(", ", containerNames)}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Found Docker container: {containerName}");
        Console.ResetColor();
        return 0;
    }
    catch (Exception ex)
    {
        return Fail($"Error checking Docker: {ex.Message}");
    }
}

var dockerCheckResult = CheckDockerContainer($"sql2022-bootcamp-tests-{databaseName}");
if (dockerCheckResult != 0)
{
    return dockerCheckResult;
}

#endregion

#region Build connection string

// TODO [TO20251111] Should use the environment variable connection if available. Note that we need TrustServerCertificate=True for local dev with self-signed certs.
// TODO [TO20251111] Note the hardcoded username.
var builder = new SqlConnectionStringBuilder
{
    DataSource = $"{serverName}",
    InitialCatalog = databaseName,
    UserID = "sa",
    Password = databaseName,
    TrustServerCertificate = true,
    IntegratedSecurity = false,
    Encrypt = false
};
var connectionString = builder.ConnectionString;
EnsureDatabase.For.SqlDatabase(connectionString);

#endregion


// 1) RunOnce scripts: Create + Update (journaled)
var runOnce = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsFromFileSystem(Path.Join(scriptDir, "Create"))
    .WithScriptsFromFileSystem(Path.Join(scriptDir, "Update"))
    .LogToConsole()
    .Build();

var runOnceResult = runOnce.PerformUpgrade();
if (!runOnceResult.Successful)
{
    return Fail(runOnceResult?.Error.ToString() ?? "Could not run scripts to create and update database.");
}

// 2) RunAlways scripts: things to re-apply each run (procs/views/perms)
var runAlways = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsFromFileSystem(Path.Join(scriptDir, "Everytime"),
        new SqlScriptOptions { ScriptType = ScriptType.RunAlways })
    .JournalTo(new NullJournal())
    .LogToConsole()
    .Build();

var runAlwaysResult = runAlways.PerformUpgrade();
if (!runAlwaysResult.Successful)
{
    return Fail(runAlwaysResult?.Error.ToString() ?? "Failed to re-apply RunAlways scripts.");
}


// 3) Optional test data pass (journaled or not, your choice)
var testData = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsFromFileSystem(Path.Join(scriptDir, "TestData"))
    .LogToConsole()
    .Build();

var testDataResult = testData.PerformUpgrade();
if (!testDataResult.Successful)
{
    return Fail(testDataResult?.Error.ToString() ?? "Failed to run TestData scripts.");
}


Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Finished updating {databaseName}.");
Console.ResetColor();
return 0;