namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
public sealed record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyList<EnvironmentVariableEntry> EnvironmentVariables);

/// <summary>
/// A redacted environment variable name/value pair for runtime diagnostics.
/// </summary>
public sealed record EnvironmentVariableEntry(
    string Name,
    string Value);
