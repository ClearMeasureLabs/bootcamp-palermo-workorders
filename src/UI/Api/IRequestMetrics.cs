namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks aggregate HTTP request counts for runtime metrics.
/// </summary>
public interface IRequestMetrics
{
    /// <summary>
    /// Total number of HTTP requests served since process start.
    /// </summary>
    long TotalRequestsServed { get; }

    /// <summary>
    /// Increments the served-request counter by one.
    /// </summary>
    void IncrementTotalRequestsServed();
}
