namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
/// <param name="OsDescription">Operating system description from the runtime.</param>
/// <param name="ProcessorCount">Logical processor count for the current process.</param>
/// <param name="ClrVersion">CLR version string for the running application.</param>
/// <param name="EnvironmentVariables">Curated environment variable names mapped to redacted placeholder values.</param>
public sealed record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyDictionary<string, string> EnvironmentVariables);
