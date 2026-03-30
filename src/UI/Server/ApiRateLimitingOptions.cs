namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for sliding-window rate limiting on <c>/api/*</c> and <c>/api/blazor-wasm-single-api</c> routes.
/// </summary>
public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "ApiRateLimiting";

    /// <summary>
    /// When false, registration still occurs but all requests are unlimited.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests allowed per client per window when limiting is enabled.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Sliding window length.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Number of segments the window is divided into (higher = smoother sliding).
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>
    /// Queued permits when the window is full; 0 means fail immediately.
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Optional header for per-client key; when absent, connection remote IP is used.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
}
