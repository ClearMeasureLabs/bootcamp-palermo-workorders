namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks total HTTP requests served by the host process.
/// </summary>
public interface IRequestMetrics
{
    /// <summary>
    /// Total requests counted since process start.
    /// </summary>
    long TotalRequestsServed { get; }

    /// <summary>
    /// Records one completed HTTP request.
    /// </summary>
    void Increment();
}
