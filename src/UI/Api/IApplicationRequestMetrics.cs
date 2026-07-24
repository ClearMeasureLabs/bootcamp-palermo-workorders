namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks total HTTP requests handled by the host process.
/// </summary>
public interface IApplicationRequestMetrics
{
    /// <summary>
    /// Total requests observed since process start.
    /// </summary>
    long TotalRequests { get; }

    /// <summary>
    /// Increments the request counter by one.
    /// </summary>
    void Increment();
}
