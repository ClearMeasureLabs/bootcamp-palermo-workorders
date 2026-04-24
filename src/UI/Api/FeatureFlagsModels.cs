namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON contract for one entry in <c>GET /api/features/flags</c>.
/// </summary>
public sealed record FeatureFlagItem(string Name, bool Enabled);

/// <summary>
/// JSON payload for <c>GET /api/features/flags</c> and <c>GET /api/v1.0/features/flags</c>.
/// </summary>
public sealed record FeatureFlagsResponse(IReadOnlyList<FeatureFlagItem> Flags);
