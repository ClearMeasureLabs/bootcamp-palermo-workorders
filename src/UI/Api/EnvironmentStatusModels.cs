namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    string FrameworkDescription,
    IReadOnlyList<EnvironmentVariableStatusEntry> EnvironmentVariables);

/// <summary>
/// Presence of a single allowlisted environment variable without exposing its value.
/// </summary>
public record EnvironmentVariableStatusEntry(string Name, bool IsSet);
