namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Resolves current enabled/disabled status for all cataloged feature flags from configuration options.
/// </summary>
public static class FeatureFlagStatusResolver
{
    /// <summary>
    /// Returns a flat dictionary of catalog keys to current boolean values from <paramref name="options"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> Resolve(DiagnosticsFeatureFlagsOptions options)
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var (key, getter) in FeatureFlagCatalog.Entries)
            result[key] = getter(options);
        return result;
    }
}
