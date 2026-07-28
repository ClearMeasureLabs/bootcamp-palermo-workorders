namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration for <c>GET /api/status/environment</c>: bounded allowlist of environment variable names (values never returned).
/// </summary>
public sealed class RuntimeEnvironmentStatusOptions
{
    /// <summary>Configuration section name (root <c>RuntimeEnvironmentStatus</c> in appsettings).</summary>
    public const string SectionName = "RuntimeEnvironmentStatus";

    /// <summary>Maximum number of variable names honored from configuration (including defaults).</summary>
    public const int MaxVariableNames = 32;

    /// <summary>
    /// Names to report with redacted values only. When empty or omitted, <see cref="DefaultVariableNames"/> is used.
    /// </summary>
    public string[] VariableNames { get; set; } = [];

    /// <summary>Safe default allowlist when <see cref="VariableNames"/> is empty.</summary>
    public static readonly string[] DefaultVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_RUNNING_IN_CONTAINER",
        "WEBSITE_SITE_NAME",
        "COMPUTERNAME",
        "HOSTNAME"
    ];
}
