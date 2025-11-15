using DbUp;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// This should match the AliaSQL "Update" action, which only runs Update.
/// </summary>
[UsedImplicitly]
public class UpdateDatabaseCommand() : BaseDatabaseCommand("Update")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        ShowOptionsOnConsole(options);
        var scriptDir = Path.Join( GetScriptDirectory(options), "Update");
        var connectionString= GetConnectionString(options);
        var upgradeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptDir)
            .LogToConsole()
            .Build();

        var result = upgradeEngine.PerformUpgrade();
        return !result.Successful ? Fail(result?.Error?.ToString() ?? "Could not run scripts to update database.") : 0;
    }
}