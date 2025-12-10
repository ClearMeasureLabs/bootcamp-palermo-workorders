using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.CLI;

/// <summary>
///     Drops the specified database if it exists.
/// </summary>
[UsedImplicitly]
public class DropDatabaseCommand(IDatabaseTasks dbTasks) : AbstractDatabaseCommand("Drop")
{
    protected override async Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var result1 = await dbTasks.DropDatabaseAsync(GetMasterConnectionString(options), options.DatabaseName,
            cancellationToken);
        return result1 != 0 ? result1 : 0;
    }
}