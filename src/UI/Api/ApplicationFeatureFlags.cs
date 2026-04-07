using System.Collections.Frozen;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory registry of application feature flags and their default enabled state.
/// </summary>
public static class ApplicationFeatureFlags
{
    private static readonly FrozenDictionary<string, bool> Flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        ["RealtimeNotifications"] = true,
        ["WorkOrderBulkImport"] = true,
        ["ExperimentalAiChat"] = false
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All defined flags keyed by name with the current enabled value.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> All => Flags;
}
