using System.Collections.Frozen;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory feature flags exposed on <c>GET /api/features/flags</c>.
/// Separate from <see cref="DiagnosticsFeatureFlagsOptions"/> (configuration-bound diagnostics flags).
/// </summary>
public static class ApplicationFeatureFlags
{
    private static readonly FrozenDictionary<string, bool> Flags = new Dictionary<string, bool>
    {
        ["sampleFeatureA"] = true,
        ["sampleFeatureB"] = false
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns all application feature flags and their enabled/disabled state.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> GetAll() => Flags;
}
