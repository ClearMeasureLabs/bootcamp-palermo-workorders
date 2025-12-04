using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Drops the specified database if it exists.
/// </summary>
[UsedImplicitly]
public class DropDatabaseCommand() : AbstractDatabaseCommand("Drop")
{
    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        var databaseName = options.DatabaseName;
        
        try
        {
            // Check if database exists
            var masterConnectionString = GetMasterConnectionString(options);

            using var connection = new SqlConnection(masterConnectionString);
            connection.Open();
                
            // Check if database exists
            var checkCommand = new SqlCommand(
                "SELECT database_id FROM sys.databases WHERE Name = @DatabaseName", 
                connection);
            checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                
            var result = checkCommand.ExecuteScalar();
                
            if (result == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Database '{databaseName.EscapeMarkup()}' does not exist. Nothing to drop.[/]");
                return 0;
            }
                
            // Set database to single user mode to drop connections
            AnsiConsole.MarkupLine($"[dim]Setting database to single user mode...[/]");
            var setSingleUserCommand = new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", 
                connection);
            setSingleUserCommand.ExecuteNonQuery();
                
            // Drop the database
            AnsiConsole.MarkupLine($"[yellow]Dropping database '{databaseName.EscapeMarkup()}'...[/]");
            var dropCommand = new SqlCommand($"DROP DATABASE [{databaseName}]", connection);
            dropCommand.ExecuteNonQuery();
                
            AnsiConsole.MarkupLine($"[green]Database '{databaseName.EscapeMarkup()}' dropped successfully.[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return Fail($"Failed to drop database '{databaseName}': {ex.Message}");
        }
    }
    
    private static string GetMasterConnectionString(DatabaseOptions options)
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
}

