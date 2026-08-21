using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds the runtime environment status snapshot with redacted configuration values.
/// </summary>
public static class EnvironmentStatusResponseBuilder
{
    /// <summary>
    /// Placeholder returned for every curated environment variable value.
    /// </summary>
    public const string RedactedEnvironmentVariableValue = "(redacted)";

    private static readonly string[] CuratedEnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model",
        "ApiKeyAuthentication__Enabled",
        "ApiKeyAuthentication__ValidationKey",
        "OTEL_EXPORTER_OTLP_ENDPOINT"
    ];

    /// <summary>
    /// Returns OS, processor, CLR metadata and a curated dictionary of redacted environment variable values.
    /// </summary>
    /// <param name="configuration">Application configuration used to resolve curated keys.</param>
    public static EnvironmentStatusResponse Build(IConfiguration configuration)
    {
        var environmentVariables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in CuratedEnvironmentVariableNames)
        {
            _ = ResolveConfiguredValue(configuration, name);
            environmentVariables[name] = RedactedEnvironmentVariableValue;
        }

        return new EnvironmentStatusResponse(
            RuntimeInformation.OSDescription,
            Environment.ProcessorCount,
            Environment.Version.ToString(),
            environmentVariables);
    }

    private static string? ResolveConfiguredValue(IConfiguration configuration, string name)
    {
        var configurationKey = name.Replace("__", ":", StringComparison.Ordinal);
        var fromConfiguration = configuration[configurationKey];
        if (!string.IsNullOrEmpty(fromConfiguration))
            return fromConfiguration;

        var directConfiguration = configuration[name];
        if (!string.IsNullOrEmpty(directConfiguration))
            return directConfiguration;

        return Environment.GetEnvironmentVariable(name);
    }
}
