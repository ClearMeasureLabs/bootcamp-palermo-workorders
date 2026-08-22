namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Process-scoped counter of completed HTTP requests for operator metrics.
/// </summary>
public interface IRequestMetricsCounter
{
    /// <summary>
    /// Current total of completed requests since process start.
    /// </summary>
    long TotalRequestsServed { get; }

    /// <summary>
    /// Records one completed HTTP request.
    /// </summary>
    void RecordRequest();
}
