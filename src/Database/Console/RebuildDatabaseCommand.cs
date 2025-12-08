using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using DbUp.Support;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// This should resemble the AliaSQL "Rebuild" action, which runs Create, Update, Everytime, and TestData scripts.
/// </summary>
[UsedImplicitly]
public class RebuildDatabaseCommand() : AbstractDatabaseCommand("Rebuild")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        
        var connectionString = GetConnectionString(options);

        // 1) RunOnce scripts: Create + Update (journaled)
        var createAndUpdateEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Create"))
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Update"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogToConsole()
            .Build();

        var createAndUpdateResult = createAndUpdateEngine.PerformUpgrade();
        if (!createAndUpdateResult.Successful)
        {
            return Fail(createAndUpdateResult.Error?.ToString() ?? "Could not run scripts to rebuild database.");
        }

        // 2) RunAlways scripts: things to re-apply each run (procs/views/perms)
        var everytimeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Everytime"),
                new SqlScriptOptions { ScriptType = ScriptType.RunAlways })
            .JournalTo(new NullJournal())
            .LogToConsole()
            .Build();

        var everytimeResult = everytimeEngine.PerformUpgrade();
        if (!everytimeResult.Successful)
        {
            return Fail(everytimeResult.Error?.ToString() ?? "Failed to re-apply RunAlways scripts.");
        }

        // 3) Optional test data pass (journaled or not, your choice)
        var testDataEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "TestData"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogToConsole()
            .Build();

        var testDataResult = testDataEngine.PerformUpgrade();
        if (!testDataResult.Successful)
        {
            return Fail(testDataResult.Error?.ToString() ?? "Failed to run TestData scripts.");
        }

        AnsiConsole.MarkupLine($"[green]Finished updating {options.DatabaseName}.[/]");
        return 0;
    }
}