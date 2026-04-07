using System.Collections.ObjectModel;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Static in-memory feature flags for the HTTP API contract at <c>GET /api/features/flags</c>.
/// New flags are registered here only (no external provider in this work item).
/// </summary>
public static class FeatureFlagsRegistry
{
    private static readonly IReadOnlyDictionary<string, bool> AllFlags = new ReadOnlyDictionary<string, bool>(
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["SampleOperationalFlag"] = true,
        });

    /// <summary>
    /// All defined flags and their default enabled state.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> All => AllFlags;

    /// <summary>
    /// Returns an immutable snapshot of <see cref="All"/> (same content; safe for concurrent readers).
    /// </summary>
    public static IReadOnlyDictionary<string, bool> GetSnapshot() => AllFlags;
}
