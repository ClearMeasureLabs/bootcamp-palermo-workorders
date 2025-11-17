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
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.DatabaseServer,
            TrustServerCertificate = true,
            InitialCatalog = options.DatabaseName
        };

        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            return builder.ToString();
        }

        builder.UserID = options.DatabaseUser;
        builder.Password = options.DatabasePassword;

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
            $"[green]{assemblyName} migrating database {Action} {options.DatabaseServer} {options.DatabaseName} {GetScriptDirectory(options)} {userInfo}[/]");
    }
}