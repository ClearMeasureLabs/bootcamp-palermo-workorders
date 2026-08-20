using System.Runtime.InteropServices;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds a read-only runtime environment diagnostics snapshot for ops endpoints.
/// </summary>
public static class EnvironmentStatusSnapshot
{
    /// <summary>Placeholder used in place of every environment variable value.</summary>
    public const string RedactedValue = "[REDACTED]";

    /// <summary>Curated diagnostic environment variable names (values never returned).</summary>
    public static readonly IReadOnlyList<string> AllowlistedNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "DATABASE_ENGINE"
    ];

    /// <summary>
    /// Captures OS, processor count, CLR/framework description, and allowlisted env var names with redacted values.
    /// Missing allowlisted names are omitted.
    /// </summary>
    /// <param name="getEnvironmentVariable">
    /// Optional resolver (defaults to <see cref="Environment.GetEnvironmentVariable(string)"/>) for tests.
    /// </param>
    public static EnvironmentStatusResponse Build(Func<string, string?>? getEnvironmentVariable = null)
    {
        var resolve = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllowlistedNames)
        {
            if (resolve(name) is not null)
            {
                environmentVariables[name] = RedactedValue;
            }
        }

        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: RuntimeInformation.FrameworkDescription,
            EnvironmentVariables: environmentVariables);
    }
}
