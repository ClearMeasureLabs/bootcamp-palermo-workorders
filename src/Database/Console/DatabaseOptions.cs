using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

[UsedImplicitly]
public class DatabaseOptions : CommandSettings
{
    [CommandArgument(0, "<databaseServer>")]
    [Description("The database server name or address")]
    public string DatabaseServer { get; set; } = string.Empty;

    [CommandArgument(1, "<databaseName>")]
    [Description("The name of the database")]
    public string DatabaseName { get; set; } = string.Empty;

    [CommandArgument(2, "[scriptDir]")]
    [Description("The directory containing the migration scripts. Defaults to .\\scripts")]
    public string ScriptDir { get; set; } = ".\\scripts";

    [CommandArgument(3, "[databaseUser]")]
    [Description("Optional database username for authentication")]
    public string? DatabaseUser { get; set; }

    [CommandArgument(4, "[databasePassword]")]
    [Description("Optional database password for authentication")]
    public string? DatabasePassword { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseServer))
        {
            return ValidationResult.Error("Database server is required");
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            return ValidationResult.Error("Database name is required");
        }


        // If one credential is provided, both should be provided
        if (!string.IsNullOrWhiteSpace(DatabaseUser) && string.IsNullOrWhiteSpace(DatabasePassword))
        {
            return ValidationResult.Error("Database password is required when username is provided");
        }

        if (string.IsNullOrWhiteSpace(DatabaseUser) && !string.IsNullOrWhiteSpace(DatabasePassword))
        {
            return ValidationResult.Error("Database username is required when password is provided");
        }

        return ValidationResult.Success();
    }
}