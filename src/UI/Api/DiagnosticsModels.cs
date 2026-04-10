namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/diagnostics</c> and <c>GET /api/v1.0/diagnostics</c>.
/// </summary>
public sealed record DiagnosticsResponse(
    string Environment,
    TimeSpan Uptime,
    DiagnosticsFeatureFlagsOptions FeatureFlags);
