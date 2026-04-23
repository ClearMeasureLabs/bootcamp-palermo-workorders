namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Records HTTP requests handled by the host and exposes a point-in-time snapshot for
/// <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public interface IApplicationRuntimeMetricsSnapshot
{
    /// <summary>
    /// Increments the per-process counter for requests that reached the host pipeline
    /// (see <see cref="MetricsSummaryResponse.TotalRequestsServed"/>).
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// Builds the JSON payload: uptime uses the same calculation as <see cref="SimpleHealthResponseBuilder"/>;
    /// memory and GC values reflect the current process at call time.
    /// </summary>
    MetricsSummaryResponse Build(TimeProvider timeProvider);
}
