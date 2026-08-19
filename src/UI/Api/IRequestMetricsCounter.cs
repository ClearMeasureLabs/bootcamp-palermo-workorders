namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks total HTTP requests served by the host process.
/// </summary>
public interface IRequestMetricsCounter
{
    /// <summary>Total requests counted since process start.</summary>
    long TotalRequestsServed { get; }

    /// <summary>Increments the served-request count by one.</summary>
    void RecordRequestServed();
}
