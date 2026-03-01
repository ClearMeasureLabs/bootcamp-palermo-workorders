using DbUp;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// This should match the DbUp "Update" action, which only runs Update.
/// </summary>
[UsedImplicitly]
public class UpdateDatabaseCommand() : AbstractDatabaseCommand("Update")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken)
    {
        var scriptDir = Path.Join( GetScriptDirectory(options), "Update");
        var upgradeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptDir)
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(new QuietLog())
            .Build();

        var result = upgradeEngine.PerformUpgrade();
        return !result.Successful ? Fail(result?.Error?.ToString() ?? "Could not run scripts to update database.") : 0;
    }
}
