namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryBuilder
{
    /// <summary>
    /// Creates a runtime metrics summary using the current process and request counters.
    /// </summary>
    public static MetricsSummaryResponse Build(
        string environmentName,
        TimeProvider timeProvider,
        IRequestMetrics requestMetrics)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        return new MetricsSummaryResponse(
            Environment: environmentName,
            Uptime: healthSlice.Uptime,
            TotalRequestsServed: requestMetrics.TotalRequestsServed,
            MemoryMb: ProcessMemoryMetrics.GetWorkingSetMb(),
            GcCollectionCounts: new GcCollectionCounts(
                Gen0Count: GC.CollectionCount(0),
                Gen1Count: GC.CollectionCount(1),
                Gen2Count: GC.CollectionCount(2)));
    }
}
