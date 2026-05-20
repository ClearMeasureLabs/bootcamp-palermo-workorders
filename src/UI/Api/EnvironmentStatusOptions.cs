namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration for <c>GET /api/status/environment</c>: which environment variable names may appear
/// (values are never exposed, only presence).
/// </summary>
public sealed class EnvironmentStatusOptions
{
    /// <summary>Configuration section name (root <c>EnvironmentStatus</c> in appsettings).</summary>
    public const string SectionName = "EnvironmentStatus";

    /// <summary>
    /// Names of environment variables to report as <see cref="EnvironmentVariableStatusEntry"/> items.
    /// Only these names are considered; full enumeration is never performed.
    /// </summary>
    public List<string> IncludedEnvironmentVariables { get; set; } = [];
}
