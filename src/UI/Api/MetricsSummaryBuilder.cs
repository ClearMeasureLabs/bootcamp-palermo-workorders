namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryBuilder
{
    /// <summary>
    /// Creates a runtime metrics snapshot using the supplied clock and request counter.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, IRequestMetricsStore store)
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider).Uptime;
        return new MetricsSummaryResponse(
            Uptime: uptime,
            TotalRequests: store.TotalRequests,
            GcMemoryMb: GetGcMemoryMb(),
            WorkingSetMb: GetWorkingSetMb(),
            GcCollections: new GcCollectionCounts(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2)));
    }

    internal static int GetGcMemoryMb() =>
        (int)Math.Round(GC.GetTotalMemory(false) / 1_048_576.0);

    internal static int GetWorkingSetMb() =>
        (int)Math.Round(Environment.WorkingSet / 1_048_576.0);
}
