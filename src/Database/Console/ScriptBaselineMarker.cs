using DbUp;
using DbUp.Engine;
using Spectre.Console;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Marks DbUp scripts as executed without running them.
/// </summary>
public static class ScriptBaselineMarker
{
    /// <summary>
    /// Marks all scripts in a directory as executed in the schema journal.
    /// </summary>
    public static int MarkScriptsInDirectory(string connectionString, string scriptPath, string scriptType)
    {
        if (!Directory.Exists(scriptPath))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Skipping {scriptType}: Directory '{scriptPath.EscapeMarkup()}' does not exist[/]");
            return 0;
        }

        var upgradeEngine = BuildUpgradeEngine(connectionString, scriptPath);
        var scripts = upgradeEngine.GetScriptsToExecute();

        if (scripts.Count == 0)
        {
            AnsiConsole.MarkupLine(
                $"[green]{scriptType}: No scripts to baseline (all already marked as executed)[/]");
            return 0;
        }

        return MarkScripts(upgradeEngine, scripts, scriptType);
    }

    private static UpgradeEngine BuildUpgradeEngine(string connectionString, string scriptPath)
    {
        return DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptPath)
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(new QuietLog())
            .Build();
    }

    private static int MarkScripts(UpgradeEngine upgradeEngine, IReadOnlyList<SqlScript> scripts, string scriptType)
    {
        AnsiConsole.MarkupLine($"[cyan]{scriptType}: Marking {scripts.Count} script(s) as executed...[/]");

        foreach (var script in scripts)
        {
            upgradeEngine.MarkAsExecuted(script.Name);
            AnsiConsole.MarkupLine($"  [dim]✓ {script.Name}[/]");
        }

        AnsiConsole.MarkupLine(
            $"[green]{scriptType}: Successfully marked {scripts.Count} script(s) as executed[/]");
        return 0;
    }
}
