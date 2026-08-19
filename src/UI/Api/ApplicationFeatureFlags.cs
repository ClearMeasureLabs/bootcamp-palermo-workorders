namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory catalog of application feature flags, hydrated from configuration at host startup.
/// </summary>
public static class ApplicationFeatureFlags
{
    private static readonly Dictionary<string, bool> KnownFlagDefaults = new(StringComparer.Ordinal)
    {
        ["SampleFeatureA"] = false,
        ["SampleFeatureB"] = false,
    };

    private static IReadOnlyDictionary<string, bool> _flags =
        new Dictionary<string, bool>(KnownFlagDefaults, StringComparer.Ordinal);

    /// <summary>
    /// Current flag names and enabled/disabled values for <c>GET /api/features/flags</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> Flags => _flags;

    /// <summary>
    /// Copies configured values from <paramref name="options"/> into the static catalog (called once at startup).
    /// </summary>
    public static void HydrateFrom(DiagnosticsFeatureFlagsOptions options)
    {
        var hydrated = new Dictionary<string, bool>(KnownFlagDefaults, StringComparer.Ordinal)
        {
            ["SampleFeatureA"] = options.SampleFeatureA,
            ["SampleFeatureB"] = options.SampleFeatureB,
        };
        _flags = hydrated;
    }
}
