using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="EnvironmentStatusResponse"/> from runtime probes and an allowlisted set of environment variables.
/// </summary>
public static class EnvironmentStatusBuilder
{
    /// <summary>Sentinel returned for every allowlisted environment variable value.</summary>
    public const string RedactedValue = "***";

    /// <summary>
    /// Captures OS, processor count, CLR version, and redacted allowlisted environment variables.
    /// </summary>
    public static EnvironmentStatusResponse Build(IOptions<EnvironmentDiagnosticsOptions> options)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in options.Value.VariableNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var value = Environment.GetEnvironmentVariable(name);
            if (value is null)
            {
                continue;
            }

            variables[name] = RedactedValue;
        }

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: RuntimeInformation.FrameworkDescription,
            EnvironmentVariables: variables);
    }
}
