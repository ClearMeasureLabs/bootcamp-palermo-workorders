using System.ComponentModel;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

[UsedImplicitly]
public class DatabaseOptions : CommandSettings
{
    [CommandArgument(0, "<databaseAction>")]
    [Description("The database action to perform (e.g., Create|Update|Rebuild|TestData|Baseline|Drop)")]

    public string DatabaseAction { get; set; } = string.Empty;

    [CommandArgument(1, "<databaseServer>")]
    [Description("The database server name or address")]
    public string DatabaseServer { get; set; } = string.Empty;

    [CommandArgument(2, "<databaseName>")]
    [Description("The name of the database")]
    public string DatabaseName { get; set; } = string.Empty;

    [CommandArgument(3, "<scriptDir>")]
    [Description("The directory containing the migration scripts")]
    public string ScriptDir { get; set; } = string.Empty;

    [CommandArgument(4, "[databaseUser]")]
    [Description("Optional database username for authentication")]
    public string? DatabaseUser { get; set; }

    [CommandArgument(5, "[databasePassword]")]
    [Description("Optional database password for authentication")]
    public string? DatabasePassword { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabaseAction))
        {
            return ValidationResult.Error("Database action is required");
        }

        if (string.IsNullOrWhiteSpace(DatabaseServer))
        {
            return ValidationResult.Error("Database server is required");
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            return ValidationResult.Error("Database name is required");
        }

        if (string.IsNullOrWhiteSpace(ScriptDir))
        {
            return ValidationResult.Error("Script directory is required");
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