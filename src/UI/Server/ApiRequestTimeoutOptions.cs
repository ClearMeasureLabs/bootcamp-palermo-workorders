namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for request timeouts on HTTP API routes (see <see cref="ApiRequestTimeoutsExtensions"/>).
/// </summary>
public sealed class ApiRequestTimeoutOptions
{
    public const string SectionName = "ApiRequestTimeouts";

    /// <summary>When false, API routes do not receive a request timeout policy.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Timeout duration for API requests when <see cref="Enabled"/> is true. Must be positive.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
