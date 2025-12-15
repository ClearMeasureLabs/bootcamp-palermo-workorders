using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;

namespace ClearMeasure.Bootcamp.Database.CLI;

public interface IDatabaseTasks
{
    Task<int> DropDatabaseAsync(string connectionString, string databaseName, CancellationToken cancellationToken);

    Task<int> UpdateDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken);

    Task<int> BaselineDatabaseAsync(string connectionString, DirectoryInfo scriptsDirectory,
        CancellationToken cancellationToken);
    
    Task<int> EnsureDatabaseExistsAsync(string connectionString, string databaseName,
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
            await using (var checkCommand = new SqlCommand(
                "SELECT database_id FROM sys.databases WHERE Name = @DatabaseName",
                connection))
            {
                checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);

                var result = await checkCommand.ExecuteScalarAsync(cancellationToken);

                if (result == null)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Database '{databaseName.EscapeMarkup()}' does not exist. Nothing to drop.[/]");
                    return 0;
                }
            }

            // Set database to single user mode to drop connections
            await using (var setSingleUserCommand = new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                connection))
            {
                await setSingleUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Drop the database
            AnsiConsole.MarkupLine($"[yellow]Dropping database '{databaseName.EscapeMarkup()}'...[/]");
            await using (var dropCommand = new SqlCommand($"DROP DATABASE [{databaseName}]", connection))
            {
                await dropCommand.ExecuteNonQueryAsync(cancellationToken);
            }

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
    
        public string GetConnectionString(DatabaseOptions options)
    {
        // Determine if this is a local server (localhost, 127.0.0.1, or LocalDB)
        var serverName = (options.DatabaseServer ?? string.Empty).Trim();
        var isLocalServer = serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                            serverName.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                            serverName.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                            serverName.Contains("LocalDb", StringComparison.OrdinalIgnoreCase) ||
                            serverName.Contains("(LocalDb)", StringComparison.OrdinalIgnoreCase) ||
                            serverName.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                            serverName.StartsWith("localhost", StringComparison.OrdinalIgnoreCase);


        // Format DataSource to use TCP on port 1433 for non-LocalDB connections
        // This forces TCP instead of Named Pipes
        var dataSource = options.DatabaseServer ?? string.Empty;
        var isLocalDb = serverName.Contains("LocalDb", StringComparison.OrdinalIgnoreCase) ||
                        serverName.Contains("(LocalDb)", StringComparison.OrdinalIgnoreCase);

        if (!isLocalDb)
        {
            // For non-LocalDB servers, ensure TCP port 1433 is specified
            // Check if port is already specified (comma, colon, or backslash indicates port/instance)
            if (!dataSource.Contains(',') && !dataSource.Contains(':') && !dataSource.Contains('\\'))
            {
                // No port specified, add port 1433 to force TCP connection
                dataSource = $"{dataSource},1433";
            }
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = options.DatabaseName,
            ConnectTimeout = 60
        };

        // Configure encryption and certificate trust based on server location
        // These must be explicitly set to ensure DbUp preserves them when creating master connections
        if (isLocalServer)
        {
            // Local servers: don't encrypt, trust certificate
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
        }
        else
        {
            // Remote servers or Azure SQL Database: encrypt, don't trust certificate (require proper validation)
            builder.Encrypt = true;
            builder.TrustServerCertificate = false;
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            // Use Windows Integrated Security
            builder.IntegratedSecurity = true;
        }
        else
        {
            // Use SQL Server Authentication
            if (string.IsNullOrWhiteSpace(options.DatabasePassword))
            {
                throw new ArgumentException("DatabasePassword is required when DatabaseUser is provided",
                    "DatabasePassword");
            }

            builder.IntegratedSecurity = false;
            builder.UserID = options.DatabaseUser;
            builder.Password = options.DatabasePassword;
        }

        return builder.ToString();
    }


    public Task<int> EnsureDbabaseExistsAsync(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            return Task.FromException<int>(ex);
        }

        return Task.FromResult(0);
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