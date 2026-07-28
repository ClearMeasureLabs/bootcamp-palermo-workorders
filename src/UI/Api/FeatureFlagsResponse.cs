namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/features/flags</c>. Flag keys match the static catalog (<c>camelCase</c> names).
/// </summary>
/// <param name="Flags">Map of flag identifiers to enabled state.</param>
public sealed record FeatureFlagsResponse(IReadOnlyDictionary<string, bool> Flags);
