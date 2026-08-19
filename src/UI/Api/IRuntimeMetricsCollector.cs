namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks runtime HTTP request totals and builds the metrics summary payload.
/// </summary>
public interface IRuntimeMetricsCollector
{
    /// <summary>
    /// Records one completed HTTP request (invoked by <c>RequestMetricsMiddleware</c>).
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// Builds the current metrics snapshot using process uptime, memory, GC, and request totals.
    /// </summary>
    MetricsSummaryResponse BuildSummary(TimeProvider timeProvider);
}
