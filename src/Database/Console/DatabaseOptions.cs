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
    public string DatabaseServer { get; init; } = string.Empty;

    [CommandArgument(1, "<databaseName>")]
    [Description("The name of the database")]
    public string DatabaseName { get; init; } = string.Empty;

    [CommandArgument(2, "[scriptDir]")]
    [Description("The directory containing the migration scripts. Defaults to .\\scripts")]
    public string ScriptDir { get; init; } = ".\\scripts";

    [CommandArgument(3, "[databaseUser]")]
    [Description("Optional database username for authentication")]
    public string? DatabaseUser { get; init; }

    [CommandArgument(4, "[databasePassword]")]
    [Description("Optional database password for authentication")]
    public string? DatabasePassword { get; init; }

    /// <summary>
    /// When true, DbUp upgrade logging is fully suppressed (used by unit tests exercising intentional failures).
    /// </summary>
    public bool SuppressUpgradeConsoleOutput { get; init; }

    public override ValidationResult Validate()
    {
        var serverError = ValidateRequired(DatabaseServer, "Database server is required");
        if (serverError is not null)
        {
            return serverError;
        }

        var nameError = ValidateRequired(DatabaseName, "Database name is required");
        if (nameError is not null)
        {
            return nameError;
        }

        return ValidateCredentials();
    }

    private static ValidationResult? ValidateRequired(string value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? ValidationResult.Error(message) : null;
    }

    private ValidationResult ValidateCredentials()
    {
        var hasUser = !string.IsNullOrWhiteSpace(DatabaseUser);
        var hasPassword = !string.IsNullOrWhiteSpace(DatabasePassword);

        if (hasUser == hasPassword)
        {
            return ValidationResult.Success();
        }

        return hasUser
            ? ValidationResult.Error("Database password is required when username is provided")
            : ValidationResult.Error("Database username is required when password is provided");
    }
}