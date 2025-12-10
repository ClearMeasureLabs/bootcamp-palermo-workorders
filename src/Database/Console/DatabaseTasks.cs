using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace ClearMeasure.Bootcamp.Database.Console;

public interface IDatabaseTasks
{
    Task<int> DropDatabaseAsync(string connectionString, string databaseName, CancellationToken cancellationToken);

    Task<int> UpdateDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken);

    Task<int> BaselineDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken);
}

public class DatabaseTasks : IDatabaseTasks
{
    private readonly string[] _scriptFolders = ["Create", "Update"];

    public async Task<int> DropDatabaseAsync(string connectionString, string databaseName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if database exists
            var checkCommand = new SqlCommand(
                "SELECT database_id FROM sys.databases WHERE Name = @DatabaseName",
                connection);
            checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);

            var result = await checkCommand.ExecuteScalarAsync(cancellationToken);

            if (result == null)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Database '{databaseName.EscapeMarkup()}' does not exist. Nothing to drop.[/]");
                return 0;
            }

            // Set database to single user mode to drop connections
            AnsiConsole.MarkupLine("[dim]Setting database to single user mode...[/]");
            var setSingleUserCommand = new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                connection);
            await setSingleUserCommand.ExecuteNonQueryAsync(cancellationToken);

            // Drop the database
            AnsiConsole.MarkupLine($"[yellow]Dropping database '{databaseName.EscapeMarkup()}'...[/]");
            var dropCommand = new SqlCommand($"DROP DATABASE [{databaseName}]", connection);
            await dropCommand.ExecuteNonQueryAsync(cancellationToken);

            AnsiConsole.MarkupLine($"[green]Database '{databaseName.EscapeMarkup()}' dropped successfully.[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return AbstractDatabaseCommand.CouldNotDropDatabase;
        }
    }

    public async Task<int> UpdateDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken)
    {
        var scriptDir = Path.Join(scriptsDirectory.FullName, "Update");
        var upgradeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptDir)
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogToConsole()
            .Build();

        var result = upgradeEngine.PerformUpgrade();
        if (!result.Successful)
        {
            AnsiConsole.WriteException(result.Error);
            return AbstractDatabaseCommand.FailedToUpdateDatabase;
        }

        AnsiConsole.MarkupLine("[green]Database update successful![/]");
        return 0;
    }

    public async Task<int> BaselineDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error ensuring database exists: {ex.Message.EscapeMarkup()}[/]");
            return AbstractDatabaseCommand.CouldNotCreateOrConnectToDatabase;
        }

        var response = 0;
        foreach (var script in _scriptFolders)
        {
            var dir = Path.Join(scriptsDirectory.FullName, "Baseline");
            var result = MarkScriptsAsExecuted(connectionString, dir, script);
            if (result != 0)
            {
                AnsiConsole.MarkupLine($"[red]Error with baselining scripts in {dir}: {result}[/]");
                response = AbstractDatabaseCommand.FailedToBaselineDatabase;
            }
        }

        return response;
    }

    private int MarkScriptsAsExecuted(string connectionString, string scriptPath, string scriptType)
    {
        if (!Directory.Exists(scriptPath))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Skipping {scriptType}: Directory '{scriptPath.EscapeMarkup()}' does not exist[/]");
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