using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="EnvironmentStatusResponse"/> snapshots with mandatory value redaction.
/// </summary>
public static class EnvironmentStatusBuilder
{
    /// <summary>Token substituted for any set environment variable value.</summary>
    public const string RedactedValue = "<redacted>";

    private static readonly string[] DefaultMonitoredVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model"
    ];

    /// <summary>
    /// Builds a runtime environment snapshot using BCL APIs and curated, redacted environment variables.
    /// </summary>
    public static EnvironmentStatusResponse Build(
        IHostEnvironment hostEnvironment,
        EnvironmentStatusOptions? options = null)
    {
        var monitoredNames = CollectMonitoredVariableNames(options);
        var environmentVariables = BuildRedactedEnvironmentVariables(monitoredNames);
        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            HostEnvironmentName: hostEnvironment.EnvironmentName,
            EnvironmentVariables: environmentVariables);
    }

    internal static IReadOnlyList<string> CollectMonitoredVariableNames(EnvironmentStatusOptions? options)
    {
        var names = new HashSet<string>(DefaultMonitoredVariableNames, StringComparer.Ordinal);
        if (options?.MonitoredVariables is { Count: > 0 } extras)
        {
            foreach (var name in extras)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name.Trim());
                }
            }
        }

        return names.OrderBy(n => n, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyDictionary<string, string> BuildRedactedEnvironmentVariables(
        IEnumerable<string> monitoredNames)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in monitoredNames)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (value is not null)
            {
                result[name] = RedactedValue;
            }
        }

        return result;
    }
}
