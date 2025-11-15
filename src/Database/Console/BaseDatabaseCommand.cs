using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

public abstract class BaseDatabaseCommand(string action) : Command<DatabaseOptions>
{
    protected readonly string _action = action;

    
    protected string GetScriptDirectory(DatabaseOptions options)
    {
        var scriptDir = options.ScriptDir
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        return scriptDir;
    }

    public override int Execute(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        ShowOptionsOnConsole(options);
        var connectionString = GetConnectionString(options);
        AnsiConsole.MarkupLine($"[green]Using connection string `{connectionString}`.[/]");

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

    protected abstract int ExecuteInternal(CommandContext context, DatabaseOptions options,
        CancellationToken cancellationToken);
    
    protected string GetConnectionString(DatabaseOptions options)
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

    protected int Fail(string message, int code = -1)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
        return code;
    }

    protected void ShowOptionsOnConsole(DatabaseOptions options)
    {
        // Display the parameters for confirmation
        AnsiConsole.MarkupLine($"[green]Action:[/] {_action}");
        AnsiConsole.MarkupLine($"[green]Server:[/] {options.DatabaseServer}");
        AnsiConsole.MarkupLine($"[green]Database:[/] {options.DatabaseName}");
        AnsiConsole.MarkupLine($"[green]Script Directory:[/] {GetScriptDirectory(options)}");
        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            return;
        }

        AnsiConsole.MarkupLine($"[green]User:[/] {options.DatabaseUser}");
        AnsiConsole.MarkupLine(
            $"[gray]Password:[/] {(string.IsNullOrEmpty(options.DatabasePassword) ? "(empty)" : "******")}");

    }
}