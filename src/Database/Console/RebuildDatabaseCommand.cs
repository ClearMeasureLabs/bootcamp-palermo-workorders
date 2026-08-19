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
        var steps = new (Func<DatabaseResult> Run, string FallbackMessage)[]
        {
            (() => DatabaseRebuildSteps.RunCreateAndUpdate(connectionString, scriptDir),
                "Could not run scripts to rebuild database."),
            (() => DatabaseRebuildSteps.RunEverytime(connectionString, scriptDir),
                "Failed to re-apply RunAlways scripts."),
            (() => DatabaseRebuildSteps.RunTestData(connectionString, scriptDir),
                "Failed to run TestData scripts.")
        };

        foreach (var (run, fallbackMessage) in steps)
        {
            var result = run();
            if (!result.Successful)
            {
                return Fail(result.ErrorMessage ?? fallbackMessage);
            }
        }

        AnsiConsole.MarkupLine($"[green]Finished updating {options.DatabaseName}.[/]");
        return 0;
    }
}
