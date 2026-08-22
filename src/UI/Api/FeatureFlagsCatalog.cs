namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory catalog of application feature flags and their runtime enabled status.
/// Distinct from configuration-bound <see cref="DiagnosticsFeatureFlagsOptions"/>.
/// </summary>
public static class FeatureFlagsCatalog
{
    private static readonly IReadOnlyDictionary<string, bool> Flags =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["EnableAdvancedSearch"] = true,
            ["EnableLegacyReports"] = false
        };

    /// <summary>
    /// Returns every seeded feature flag name and its current enabled/disabled status.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> GetAll() => Flags;
}
