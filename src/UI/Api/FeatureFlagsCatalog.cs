namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory catalog of application feature flags and their runtime enabled/disabled status.
/// </summary>
public static class FeatureFlagsCatalog
{
    private static readonly IReadOnlyDictionary<string, bool> Flags =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["SampleFeatureA"] = true,
            ["SampleFeatureB"] = false
        };

    /// <summary>
    /// All known feature flags and whether each is currently enabled.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> All => Flags;
}
