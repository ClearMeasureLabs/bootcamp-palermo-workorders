namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and <c>GET /api/v1.0/status/environment</c>.
/// </summary>
/// <param name="OsDescription">Operating system description from <see cref="System.Runtime.InteropServices.RuntimeInformation.OSDescription"/>.</param>
/// <param name="ProcessorCount">Logical processor count from <see cref="Environment.ProcessorCount"/>.</param>
/// <param name="ClrVersion">CLR version from <see cref="Environment.Version"/>.</param>
/// <param name="HostEnvironmentName">ASP.NET Core host environment name.</param>
/// <param name="EnvironmentVariables">Curated environment variable names with redacted values only.</param>
public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    string HostEnvironmentName,
    IReadOnlyDictionary<string, string> EnvironmentVariables);
