using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.CLI;

/// <summary>
///     This should resemble the AliaSQL "Rebuild" action, which runs Create, Update, Everytime, and TestData scripts.
/// </summary>
[UsedImplicitly]
public class RebuildDatabaseCommand() : AbstractDatabaseCommand("Rebuild")
{
    private readonly IDatabaseTasks _databaseTasks = null!;

    public RebuildDatabaseCommand(IDatabaseTasks databaseTasks) : this()
    {
        _databaseTasks = databaseTasks ?? throw new ArgumentNullException(nameof(databaseTasks));
    }

    protected override async Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        if (!Path.Exists(scriptDir))
        {
            throw new DirectoryNotFoundException($"Script directory '{scriptDir}' does not exist.");
        }
        
        if (string.IsNullOrEmpty(options.DatabaseName))
        {
            throw new InvalidOperationException("The database name is required for REBUILD.");
        }        
        
        var result1 = await _databaseTasks.DropDatabaseAsync(GetMasterConnectionString(options), options.DatabaseName,
            cancellationToken);
        if (result1 != 0)
        {
            return result1;
        }

        await _databaseTasks.EnsureDatabaseExistsAsync(GetConnectionString(options), options.DatabaseName,cancellationToken);

        var scriptsDir = Path.GetFullPath(options.ScriptDir!);
        var result2 = await _databaseTasks.UpdateDatabaseAsync(GetConnectionString(options),
            new DirectoryInfo(scriptsDir), cancellationToken);
        return result2 != 0 ? result2 : 0;
    }
}