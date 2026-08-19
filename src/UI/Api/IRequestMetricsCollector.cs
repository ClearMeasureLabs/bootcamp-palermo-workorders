namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks total HTTP requests served by the host since process start.
/// </summary>
public interface IRequestMetricsCollector
{
    /// <summary>
    /// Records one HTTP request.
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// Total requests recorded since process start.
    /// </summary>
    long TotalRequestsServed { get; }
}
