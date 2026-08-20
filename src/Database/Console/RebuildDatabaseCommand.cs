using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// This should resemble the DbUp "Rebuild" action, which runs Create, Update, Everytime, and TestData scripts.
/// </summary>
[UsedImplicitly]
public class RebuildDatabaseCommand() : AbstractDatabaseCommand("Rebuild")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        var result = DatabaseRebuildSteps.RunFullRebuild(connectionString, scriptDir);
        if (!result.Successful)
        {
            return Fail(result.ErrorMessage ?? "Could not run scripts to rebuild database.");
        }

        AnsiConsole.MarkupLine($"[green]Finished updating {options.DatabaseName}.[/]");
        return 0;
    }
}
