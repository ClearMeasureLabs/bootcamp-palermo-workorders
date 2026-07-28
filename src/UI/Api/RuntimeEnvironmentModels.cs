namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
public sealed record RuntimeEnvironmentResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    string FrameworkDescription,
    IReadOnlyList<RuntimeEnvironmentVariableEntry> EnvironmentVariables);

/// <summary>
/// One allowlisted process environment variable: name and redaction flag only (no raw value).
/// </summary>
public sealed record RuntimeEnvironmentVariableEntry(string Name, bool ValueRedacted);
