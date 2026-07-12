namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds a flat feature-flag snapshot from <see cref="DiagnosticsFeatureFlagsOptions"/>.
/// New flags require updates to both <see cref="DiagnosticsFeatureFlagsOptions"/> and this catalog.
/// </summary>
public static class FeatureFlagsSnapshot
{
    /// <summary>Canonical API keys exposed by <c>GET /api/features/flags</c> (camelCase JSON property names).</summary>
    public static readonly IReadOnlyList<string> CatalogKeys = ["sampleFeatureA", "sampleFeatureB"];

    /// <summary>
    /// Returns a new dictionary mapping catalog keys to current option values.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> FromOptions(DiagnosticsFeatureFlagsOptions options) =>
        new Dictionary<string, bool>
        {
            ["sampleFeatureA"] = options.SampleFeatureA,
            ["sampleFeatureB"] = options.SampleFeatureB
        };
}
