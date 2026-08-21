namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static catalog of feature flag identifiers exposed via <c>GET /api/features/flags</c>;
/// values are read from bound <see cref="DiagnosticsFeatureFlagsOptions"/>.
/// </summary>
internal static class FeatureFlagsCatalog
{
    /// <summary>JSON key aligned with camelCase serialization of <see cref="DiagnosticsFeatureFlagsOptions.SampleFeatureA"/>.</summary>
    internal const string SampleFeatureAKey = "sampleFeatureA";

    /// <summary>JSON key aligned with camelCase serialization of <see cref="DiagnosticsFeatureFlagsOptions.SampleFeatureB"/>.</summary>
    internal const string SampleFeatureBKey = "sampleFeatureB";

    /// <summary>All exposed flag keys in stable sort order (matches snapshot iteration order).</summary>
    internal static readonly IReadOnlyList<string> AllKeys = new[]
    {
        SampleFeatureAKey,
        SampleFeatureBKey
    };

    /// <summary>
    /// Builds a sorted map of catalog keys to current configuration values for JSON responses.
    /// </summary>
    internal static SortedDictionary<string, bool> BuildSnapshot(DiagnosticsFeatureFlagsOptions options) =>
        new(StringComparer.Ordinal)
        {
            [SampleFeatureAKey] = options.SampleFeatureA,
            [SampleFeatureBKey] = options.SampleFeatureB
        };
}
