using DbUp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

public abstract class AbstractDatabaseCommand : Command<DatabaseOptions>
{
    protected static string GetScriptDirectory(DatabaseOptions options)
    {
        return Path.GetFullPath(options.ScriptDir);
    }

    public override int Execute(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString(options);
        try
        {
            EnsureDatabase.For.SqlDatabase(connectionString, DatabaseUpgradeLogSelector.ForOptions(options));
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }

        return ExecuteInternal(context, options, connectionString, cancellationToken);
    }

    // ReSharper disable UnusedParameter.Global
    protected abstract int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken);
    // ReSharper restore UnusedParameter.Global

    private static string GetConnectionString(DatabaseOptions options)
    {
        return DatabaseConnectionStringBuilder.Build(options);
    }

    protected static int Fail(string message, int code = -1)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
        return code;
    }
}
