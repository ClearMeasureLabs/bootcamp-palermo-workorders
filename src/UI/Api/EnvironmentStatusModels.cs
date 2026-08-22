namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// Environment variable values are always redacted placeholders — never real secrets.
/// </summary>
public sealed record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyDictionary<string, string> EnvironmentVariables);
