namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory registry of runtime feature flags exposed on <c>GET /api/features/flags</c>.
/// </summary>
public static class FeatureFlagRegistry
{
    private static readonly Dictionary<string, bool> Flags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sampleFeatureA"] = false,
        ["sampleFeatureB"] = false
    };

    /// <summary>
    /// Returns a snapshot of all registered flags and their current enabled state.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> GetSnapshot() =>
        new Dictionary<string, bool>(Flags);

    /// <summary>
    /// Copies configuration-bound sample flags into the registry so appsettings and the static map stay aligned.
    /// </summary>
    public static void HydrateFrom(DiagnosticsFeatureFlagsOptions options)
    {
        Flags["sampleFeatureA"] = options.SampleFeatureA;
        Flags["sampleFeatureB"] = options.SampleFeatureB;
    }
}
