using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
///     This should resemble the AliaSQL "Rebuild" action, which runs Create, Update, Everytime, and TestData scripts.
/// </summary>
[UsedImplicitly]
public class RebuildDatabaseCommand() : AbstractDatabaseCommand("Rebuild")
{
    private readonly IDatabaseTasks _databaseTasks;

    public RebuildDatabaseCommand(IDatabaseTasks databaseTasks) : this()
    {
        _databaseTasks = databaseTasks;
    }

    protected override async Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var result1 = await _databaseTasks.DropDatabaseAsync(GetMasterConnectionString(options), options.DatabaseName,
            cancellationToken);
        if (result1 != 0)
        {
            return result1;
        }

        await _databaseTasks.EnsureDbabaseExistsAsync(GetConnectionString(options), options.DatabaseName,cancellationToken);

        var scriptsDir = Path.GetFullPath(options.ScriptDir!);
        var result2 = await _databaseTasks.UpdateDatabaseAsync(GetConnectionString(options),
            new DirectoryInfo(scriptsDir), cancellationToken);
        return result2 != 0 ? result2 : 0;
    }
}