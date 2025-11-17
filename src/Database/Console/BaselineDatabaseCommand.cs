using DbUp;
using DbUp.Engine;
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
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        var scriptDir = GetScriptDirectory(options);
        var connectionString = GetConnectionString(options);

        AnsiConsole.MarkupLine("[yellow]Baselining database - marking all scripts as executed without running them...[/]");

        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error ensuring database exists: {ex.Message.EscapeMarkup()}[/]");
            return -1;
        }

        // Mark Create scripts as executed
        var createResult = MarkScriptsAsExecuted(connectionString, Path.Join(scriptDir, "Create"), "Create");
        if (createResult != 0)
        {
            return createResult;
        }

        // Mark Update scripts as executed
        var updateResult = MarkScriptsAsExecuted(connectionString, Path.Join(scriptDir, "Update"), "Update");
        if (updateResult != 0)
        {
            return updateResult;
        }

        AnsiConsole.MarkupLine($"[green]Successfully baselined database '{options.DatabaseName}'. All existing scripts marked as executed.[/]");
        AnsiConsole.MarkupLine("[yellow]Note: Everytime and TestData scripts are not journaled and will run on next update/rebuild.[/]");
        
        return 0;
    }

    private int MarkScriptsAsExecuted(string connectionString, string scriptPath, string scriptType)
    {
        if (!Directory.Exists(scriptPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping {scriptType}: Directory '{scriptPath.EscapeMarkup()}' does not exist[/]");
            return 0;
        }

        var upgradeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptPath)
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogToConsole()
            .Build();

        var scripts = upgradeEngine.GetScriptsToExecute();
        
        if (scripts.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]{scriptType}: No scripts to baseline (all already marked as executed)[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[cyan]{scriptType}: Marking {scripts.Count} script(s) as executed...[/]");

        foreach (var script in scripts)
        {
            upgradeEngine.MarkAsExecuted(script.Name);
            AnsiConsole.MarkupLine($"  [dim]✓ {script.Name}[/]");
        }

        AnsiConsole.MarkupLine($"[green]{scriptType}: Successfully marked {scripts.Count} script(s) as executed[/]");
        return 0;
    }
}

