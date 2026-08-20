namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Compile-time feature flag defaults exposed on <c>GET /api/features/flags</c>.
/// Distinct from configuration-bound <see cref="DiagnosticsFeatureFlagsOptions"/> on <c>/api/diagnostics</c>.
/// </summary>
public static class ApplicationFeatureFlags
{
    /// <summary>
    /// All application feature flags and their compile-time enabled/disabled defaults.
    /// Keys are camelCase for JSON serialization.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> All { get; } =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["sampleFeatureA"] = true,
            ["sampleFeatureB"] = false
        };
}
