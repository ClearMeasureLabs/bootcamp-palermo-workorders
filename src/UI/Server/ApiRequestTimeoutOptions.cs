namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for request timeouts on HTTP API routes (see <see cref="ApiRequestTimeoutsExtensions"/>).
/// </summary>
public sealed class ApiRequestTimeoutOptions
{
    public const string SectionName = "ApiRequestTimeouts";

    /// <summary>When false, API routes do not receive a request timeout policy.</summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- required for IOptions<T> configuration binding
    public bool Enabled { get; set; } = true;

    /// <summary>Timeout duration for API requests when <see cref="Enabled"/> is true. Must be positive.</summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- required for IOptions<T> configuration binding
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Timeout used when registering the API policy: <see cref="TimeoutSeconds"/> clamped to 1..600.
    /// Values of 0 or less keep today's meaning (no timeout policy).
    /// </summary>
    public int EffectiveTimeoutSeconds =>
        TimeoutSeconds <= 0 ? TimeoutSeconds : Math.Clamp(TimeoutSeconds, 1, 600);
}
