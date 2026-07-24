namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryResponseBuilder
{
    /// <summary>
    /// Creates a snapshot of runtime metrics from the current process and request counter.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        IApplicationRequestMetrics requestMetrics,
        IProcessRuntimeMetrics processRuntimeMetrics)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        return new MetricsSummaryResponse(
            Uptime: healthSlice.Uptime,
            TotalRequests: requestMetrics.TotalRequests,
            MemoryUsageBytes: processRuntimeMetrics.WorkingSetBytes,
            GcCollections: processRuntimeMetrics.GcCollections);
    }
}
