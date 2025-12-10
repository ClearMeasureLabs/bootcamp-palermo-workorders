using System.Reflection;
using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

public abstract class AbstractDatabaseCommand(string action) : AsyncCommand<DatabaseOptions>
{
    internal const int CouldNotCreateOrConnectToDatabase = -1;
    internal const int ScriptDirectoryDoesNotExist = -2;
    internal const int CouldNotDropDatabase = -3;
    internal const int FailedToUpdateDatabase = -4;
    internal const int FailedToBaselineDatabase = -5;

    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly string Action = action;


    protected static string GetScriptDirectory(DatabaseOptions options)
    {
        return Path.GetFullPath(options.ScriptDir);
    }

    public string GetMasterConnectionString(DatabaseOptions options)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.DatabaseServer,
            InitialCatalog = "master",
            TrustServerCertificate = true,
            Encrypt = false,
            ConnectTimeout = 60
        };

        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            // Use Windows Integrated Security
            builder.IntegratedSecurity = true;
        }
        else
        {
            // Use SQL Server Authentication
            builder.IntegratedSecurity = false;
            builder.UserID = options.DatabaseUser;
            builder.Password = options.DatabasePassword;
        }

        return builder.ToString();
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

    public override async Task<int> ExecuteAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString(options);
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return Fail(
                "Could not create or connect to the database. Please check the connection parameters and ensure the database server is running.");
        }

        var scriptDir = GetScriptDirectory(options);
        if (!Path.Exists(scriptDir))
        {
            return Fail($"Script directory '{scriptDir}' does not exist.", ScriptDirectoryDoesNotExist);
        }

        return await ExecuteInternalAsync(context, options, cancellationToken);
    }

    // ReSharper disable UnusedParameter.Global
    protected abstract Task<int> ExecuteInternalAsync(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken);
    // ReSharper restore UnusedParameter.Global


    private static int Fail(string message, int code = -1)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
        return code;
    }

    private void ShowOptionsOnConsole(DatabaseOptions options)
    {
        var assemblyName = Assembly.GetExecutingAssembly().Location;

        var userInfo = !string.IsNullOrWhiteSpace(options.DatabaseUser)
            ? $"{options.DatabaseUser} <REDACTED>"
            : string.Empty;

        AnsiConsole.MarkupLine(
            $"[green]{assemblyName} performing {Action} on database {options.DatabaseServer} {options.DatabaseName}. Script directory {GetScriptDirectory(options)} {userInfo}[/]");
    }
}