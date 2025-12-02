using System.Reflection;
using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

public abstract class AbstractDatabaseCommand(string action) : Command<DatabaseOptions>
{
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly string Action = action;


    protected static string GetScriptDirectory(DatabaseOptions options)
    {
        return Path.GetFullPath(options.ScriptDir);
    }

    public override int Execute(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        ShowOptionsOnConsole(options);
        var connectionString = GetConnectionString(options);
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }

        return ExecuteInternal(context, options, cancellationToken);
    }

    // ReSharper disable UnusedParameter.Global
    protected abstract int ExecuteInternal(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken);
    // ReSharper restore UnusedParameter.Global

    protected static string GetConnectionString(DatabaseOptions options)
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

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.DatabaseServer,
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
            AnsiConsole.MarkupLine($"[dim]Detected local server '{serverName}': Encrypt=False, TrustServerCertificate=True[/]");
        }
        else
        {
            // Remote servers or Azure SQL Database: encrypt, don't trust certificate (require proper validation)
            builder.Encrypt = true;
            builder.TrustServerCertificate = false;
            AnsiConsole.MarkupLine($"[dim]Detected remote server '{serverName}': Encrypt=True, TrustServerCertificate=False[/]");
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
                throw new ArgumentException("DatabasePassword is required when DatabaseUser is provided", "DatabasePassword");
            }
            
            builder.IntegratedSecurity = false;
            builder.UserID = options.DatabaseUser;
            builder.Password = options.DatabasePassword;
        }



        // Log connection string for debugging (password redacted)
        var logBuilder = new SqlConnectionStringBuilder(builder.ToString())
        {
            Password = "******"
        };
        AnsiConsole.MarkupLine($"[dim]Connection String: {logBuilder.ToString().EscapeMarkup()}[/]");

        return builder.ToString();
    }

    protected static int Fail(string message, int code = -1)
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