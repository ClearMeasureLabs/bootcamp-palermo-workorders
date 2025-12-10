using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.CLI;

/// <summary>
///     Baseline command marks all existing scripts as executed without actually running them.
///     This is useful when introducing DbUp to an existing database.
/// </summary>
[UsedImplicitly]
public class BaselineDatabaseCommand(IDatabaseTasks dbTasks) : AbstractDatabaseCommand("baseline")
{
    protected override async Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        var connectionString = GetConnectionString(options);
        AnsiConsole.MarkupLine(
            "[yellow]Baselining database - marking all scripts as executed without running them...[/]");

        var result =
            await dbTasks.BaselineDatabaseAsync(connectionString, new DirectoryInfo(scriptDir), cancellationToken);


        AnsiConsole.MarkupLine(
            $"[yellow]Successfully baselined database '{options.DatabaseName}'. All existing scripts marked as executed.[/]");
        AnsiConsole.MarkupLine(
            "[yellow]Note: Everytime and TestData scripts are not journaled and will run on next update/rebuild.[/]");

        return result;
    }
}