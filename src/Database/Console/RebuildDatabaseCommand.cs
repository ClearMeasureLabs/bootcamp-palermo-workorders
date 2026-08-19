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

        var createAndUpdateResult = DatabaseRebuildSteps.RunCreateAndUpdate(connectionString, scriptDir);
        if (!createAndUpdateResult.Successful)
        {
            return Fail(createAndUpdateResult.ErrorMessage ?? "Could not run scripts to rebuild database.");
        }

        var everytimeResult = DatabaseRebuildSteps.RunEverytime(connectionString, scriptDir);
        if (!everytimeResult.Successful)
        {
            return Fail(everytimeResult.ErrorMessage ?? "Failed to re-apply RunAlways scripts.");
        }

        var testDataResult = DatabaseRebuildSteps.RunTestData(connectionString, scriptDir);
        if (!testDataResult.Successful)
        {
            return Fail(testDataResult.ErrorMessage ?? "Failed to run TestData scripts.");
        }

        AnsiConsole.MarkupLine($"[green]Finished updating {options.DatabaseName}.[/]");
        return 0;
    }
}
