namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/features/flags</c> and <c>GET /api/v1.0/features/flags</c>.
/// </summary>
public sealed record RuntimeFeatureFlagsResponse(DiagnosticsFeatureFlagsOptions FeatureFlags);
