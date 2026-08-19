namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Process-local request counter and runtime metrics snapshot for operator-facing APIs.
/// </summary>
public interface IRequestMetricsSnapshot
{
    /// <summary>
    /// Records one inbound HTTP request served by Kestrel.
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// Returns the total number of recorded requests since process start.
    /// </summary>
    long TotalRequestsServed { get; }

    /// <summary>
    /// Builds the metrics summary payload for the current process state.
    /// </summary>
    MetricsSummaryResponse BuildSummary(TimeProvider timeProvider);
}
