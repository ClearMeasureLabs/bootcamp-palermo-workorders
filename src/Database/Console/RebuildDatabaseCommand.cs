using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using DbUp.Support;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

[UsedImplicitly]
public class RebuildDatabaseCommand : Command<DatabaseOptions>
{
    private string GetConnectionString(DatabaseOptions options)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.DatabaseServer,
            TrustServerCertificate = true,
            InitialCatalog = options.DatabaseName
        };

        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            return builder.ToString();
        }

        builder.UserID = options.DatabaseUser;
        builder.Password = options.DatabasePassword;

        return builder.ToString();
    }

    public override int Execute(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        // Normalize the script directory path
        var scriptDir = options.ScriptDir
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        // Display the parameters for confirmation
        AnsiConsole.MarkupLine($"[green]Action:[/] {options.DatabaseAction}");
        AnsiConsole.MarkupLine($"[green]Server:[/] {options.DatabaseServer}");
        AnsiConsole.MarkupLine($"[green]Database:[/] {options.DatabaseName}");
        AnsiConsole.MarkupLine($"[green]Script Directory:[/] {scriptDir}");

        if (!string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            AnsiConsole.MarkupLine($"[green]User:[/] {options.DatabaseUser}");
            AnsiConsole.MarkupLine(
                $"[gray]Password:[/] {(string.IsNullOrEmpty(options.DatabasePassword) ? "(empty)" : "******")}");
        }

        var connectionString = GetConnectionString(options);
        AnsiConsole.MarkupLine($"[green]Using connection string `{connectionString}`.[/]");
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }

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
            return Fail(runOnceResult.Error?.ToString() ?? "Could not run scripts to create and update database.");
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
            return Fail(runAlwaysResult.Error?.ToString() ?? "Failed to re-apply RunAlways scripts.");
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
            return Fail(testDataResult.Error?.ToString() ?? "Failed to run TestData scripts.");
        }

        AnsiConsole.MarkupLine($"[green]Finished updating {options.DatabaseName}.[/]");
        return 0;
    }

    private static int Fail(string message, int code = -1)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
        return code;
    }
}