namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Provides a snapshot of in-process HTTP request metrics for operator-facing APIs.
/// </summary>
public interface IRequestMetricsSnapshot
{
    /// <summary>
    /// Total number of HTTP requests that have entered the application pipeline.
    /// </summary>
    long TotalRequestsServed { get; }
}
