namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration for allowlisted environment variable names exposed on
/// <c>GET /api/status/environment</c> (values are always redacted).
/// </summary>
public sealed class EnvironmentDiagnosticsOptions
{
    /// <summary>Configuration section name (<c>EnvironmentDiagnostics</c> in appsettings).</summary>
    public const string SectionName = "EnvironmentDiagnostics";

    /// <summary>
    /// Environment variable names to include when set; values are never emitted in cleartext.
    /// </summary>
    public IList<string> VariableNames { get; set; } =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model"
    ];
}
