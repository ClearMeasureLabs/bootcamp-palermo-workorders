using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Assembles the runtime environment snapshot with mandatory value redaction.
/// </summary>
public static class EnvironmentStatusBuilder
{
    /// <summary>Placeholder emitted for any set monitored environment variable value.</summary>
    public const string RedactedValue = "<redacted>";

    /// <summary>Default operator-relevant environment variable names aligned with deployment runbooks.</summary>
    public static readonly string[] DefaultMonitoredVariables =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE",
        "ConnectionStrings__SqlConnectionString",
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        "AI_OpenAI_ApiKey",
        "AI_OpenAI_Url",
        "AI_OpenAI_Model",
        "APPLICATIONINSIGHTS_CONNECTION_STRING"
    ];

    /// <summary>
    /// Builds a redacted environment status snapshot from BCL APIs and the process environment.
    /// </summary>
    public static EnvironmentStatusResponse Build(IHostEnvironment hostEnvironment, EnvironmentStatusOptions? options = null)
    {
        var monitored = ResolveMonitoredVariables(options);
        var environmentVariables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in monitored)
        {
            if (Environment.GetEnvironmentVariable(name) is not null)
            {
                environmentVariables[name] = RedactedValue;
            }
        }

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            HostEnvironmentName: hostEnvironment.EnvironmentName,
            EnvironmentVariables: environmentVariables);
    }

    internal static IReadOnlyList<string> ResolveMonitoredVariables(EnvironmentStatusOptions? options)
    {
        var configured = options?.MonitoredVariables;
        if (configured is { Count: > 0 })
        {
            return configured;
        }

        return DefaultMonitoredVariables;
    }
}
