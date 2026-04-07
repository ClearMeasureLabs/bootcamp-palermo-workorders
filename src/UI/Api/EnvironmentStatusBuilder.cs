using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds the JSON payload for <c>GET /api/status/environment</c>.
/// </summary>
public static class EnvironmentStatusBuilder
{
    /// <summary>
    /// Configuration prefix for optional simulated values (integration tests). Keys are
    /// <c>EnvironmentStatus:SimulatedEnvironmentVariables:{VARIABLE_NAME}</c>. Values are never emitted; they only force <see cref="EnvironmentVariableEntry.IsSet"/>.
    /// </summary>
    public const string SimulatedEnvironmentVariablesConfigurationPrefix = "EnvironmentStatus:SimulatedEnvironmentVariables:";

    /// <summary>
    /// Names surfaced in the response; values are always redacted.
    /// </summary>
    public static readonly IReadOnlyList<string> ReportedVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT",
        "DOTNET_ROOT",
        "DOTNET_RUNNING_IN_CONTAINER",
        "WEBSITE_SITE_NAME",
        "WEBSITE_INSTANCE_ID",
        "ENV_STATUS_PROBE_SECRET"
    ];

    internal const string RedactedValueMarker = "[redacted]";

    /// <summary>
    /// Creates a snapshot of host runtime and selected environment variables (values redacted).
    /// </summary>
    public static EnvironmentStatusResponse Build(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        var variables = new List<EnvironmentVariableEntry>(ReportedVariableNames.Count);
        foreach (var name in ReportedVariableNames)
        {
            var simulated = configuration[SimulatedEnvironmentVariablesConfigurationPrefix + name];
            string? raw;
            if (!string.IsNullOrEmpty(simulated))
                raw = simulated;
            else
                raw = Environment.GetEnvironmentVariable(name);
            variables.Add(new EnvironmentVariableEntry(
                Name: name,
                IsSet: raw is not null,
                Value: RedactedValueMarker));
        }

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            HostEnvironmentName: hostEnvironment.EnvironmentName,
            EnvironmentVariables: variables);
    }
}

/// <summary>
/// JSON payload for environment diagnostics.
/// </summary>
public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    string FrameworkDescription,
    string HostEnvironmentName,
    IReadOnlyList<EnvironmentVariableEntry> EnvironmentVariables);

/// <summary>
/// One reported environment variable with a redacted value.
/// </summary>
public record EnvironmentVariableEntry(string Name, bool IsSet, string Value);
