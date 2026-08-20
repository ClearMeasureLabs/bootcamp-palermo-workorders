namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Collects allowlisted process environment variables with values redacted for operator diagnostics.
/// </summary>
public static class EnvironmentVariableSnapshotBuilder
{
    /// <summary>
    /// Placeholder emitted for every allowlisted environment variable value.
    /// </summary>
    public const string RedactedValue = "[REDACTED]";

    private static readonly string[] Allowlist =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model",
        "OTEL_EXPORTER_OTLP_ENDPOINT"
    ];

    /// <summary>
    /// Returns redacted entries for allowlisted variables that are currently set in the process environment.
    /// </summary>
    public static IReadOnlyList<EnvironmentVariableEntry> Build()
    {
        var entries = new List<EnvironmentVariableEntry>(Allowlist.Length);
        foreach (var name in Allowlist)
        {
            if (Environment.GetEnvironmentVariable(name) is not null)
            {
                entries.Add(new EnvironmentVariableEntry(name, RedactedValue));
            }
        }

        return entries;
    }
}
