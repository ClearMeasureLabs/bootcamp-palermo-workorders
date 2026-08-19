namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
/// <param name="OsDescription">Operating system description from the runtime.</param>
/// <param name="ProcessorCount">Logical processor count visible to the process.</param>
/// <param name="ClrVersion">CLR version string.</param>
/// <param name="HostEnvironmentName">ASP.NET Core host environment name.</param>
/// <param name="EnvironmentVariables">Curated environment variable names with redacted values.</param>
public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    string HostEnvironmentName,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

/// <summary>
/// Configuration for <see cref="EnvironmentStatusBuilder"/> monitored variable names.
/// </summary>
public sealed class EnvironmentStatusOptions
{
    /// <summary>Configuration section name (<c>EnvironmentStatus</c> in appsettings).</summary>
    public const string SectionName = "EnvironmentStatus";

    /// <summary>
    /// Additional environment variable names to include beyond the built-in allow-list.
    /// Values are always redacted in the response.
    /// </summary>
    public IList<string> MonitoredVariables { get; set; } = [];
}
