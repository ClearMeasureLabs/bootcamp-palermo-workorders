using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Baseline command marks all existing scripts as executed without actually running them.
/// This is useful when introducing DbUp to an existing database.
/// </summary>
[UsedImplicitly]
public class BaselineDatabaseCommand() : AbstractDatabaseCommand("baseline")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);

        AnsiConsole.MarkupLine("[yellow]Baselining database - marking all scripts as executed without running them...[/]");

        var markResult = MarkCreateAndUpdateScripts(connectionString, scriptDir);
        if (markResult != 0)
        {
            return markResult;
        }

        AnsiConsole.MarkupLine($"[green]Successfully baselined database '{options.DatabaseName}'. All existing scripts marked as executed.[/]");
        AnsiConsole.MarkupLine("[yellow]Note: Everytime and TestData scripts are not journaled and will run on next update/rebuild.[/]");

        return 0;
    }

    /// <summary>
    /// Marks Create then Update scripts as executed. Returns the first non-zero marker result.
    /// </summary>
    public static int MarkCreateAndUpdateScripts(string connectionString, string scriptDir)
    {
        var createResult = ScriptBaselineMarker.MarkScriptsInDirectory(
            connectionString, Path.Join(scriptDir, "Create"), "Create");
        if (createResult != 0)
        {
            return createResult;
        }

        return ScriptBaselineMarker.MarkScriptsInDirectory(
            connectionString, Path.Join(scriptDir, "Update"), "Update");
    }
}
