namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration for <c>GET /api/status/environment</c> monitored environment variable names.
/// </summary>
public sealed class EnvironmentStatusOptions
{
    /// <summary>Configuration section name (<c>EnvironmentStatus</c> in appsettings).</summary>
    public const string SectionName = "EnvironmentStatus";

    /// <summary>
    /// Optional override list of environment variable names to include when set.
    /// When empty, <see cref="EnvironmentStatusBuilder.DefaultMonitoredVariables"/> is used.
    /// </summary>
    public List<string> MonitoredVariables { get; set; } = [];
}
