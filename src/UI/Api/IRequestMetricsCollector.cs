namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks HTTP request volume for runtime metrics exposed at <c>GET /api/metrics/summary</c>.
/// </summary>
public interface IRequestMetricsCollector
{
    /// <summary>
    /// Records one HTTP request that reached the counting middleware.
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// Total requests recorded since process start.
    /// </summary>
    long TotalRequestsServed { get; }
}
