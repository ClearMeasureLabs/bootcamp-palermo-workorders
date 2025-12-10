using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.CLI;

/// <summary>
///     This should match the AliaSQL "Update" action, which only runs Update.
/// </summary>
[UsedImplicitly]
public class UpdateDatabaseCommand(IDatabaseTasks databaseTasks) : AbstractDatabaseCommand("Update")
{
    protected override async Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        if (!Path.Exists(scriptDir))
        {
            throw new DirectoryNotFoundException("Script directory '{scriptDir}' does not exist.");
        }
        
        if (string.IsNullOrEmpty(options.DatabaseName))
        {
            throw new InvalidOperationException("The database name is required for UPDATE.");
        }
        
        await databaseTasks.EnsureDbabaseExistsAsync(GetConnectionString(options), options.DatabaseName,cancellationToken);
        
        var result2 = await databaseTasks.UpdateDatabaseAsync(GetConnectionString(options), new DirectoryInfo(scriptDir),
            cancellationToken);
        if (result2 != 0)
        {
            return result2;
        }

        return 0;
    }
}