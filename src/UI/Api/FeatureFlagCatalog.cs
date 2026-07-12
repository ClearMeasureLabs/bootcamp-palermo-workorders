namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static catalog of known feature-flag keys and how each resolves from <see cref="DiagnosticsFeatureFlagsOptions"/>.
/// </summary>
public static class FeatureFlagCatalog
{
    /// <summary>
    /// Known feature-flag keys (camelCase JSON contract) mapped to value resolvers.
    /// </summary>
    public static IReadOnlyDictionary<string, Func<DiagnosticsFeatureFlagsOptions, bool>> Entries { get; } =
        new Dictionary<string, Func<DiagnosticsFeatureFlagsOptions, bool>>(StringComparer.Ordinal)
        {
            ["sampleFeatureA"] = options => options.SampleFeatureA,
            ["sampleFeatureB"] = options => options.SampleFeatureB,
        };
}
