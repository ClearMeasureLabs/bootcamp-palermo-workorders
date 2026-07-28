namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryResponseBuilder
{
    /// <summary>
    /// Creates a runtime metrics summary using the process start time, request snapshot, and live memory/GC reads.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, IRequestMetricsSnapshot snapshot)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);

        return new MetricsSummaryResponse
        {
            Uptime = healthSlice.Uptime,
            TotalRequestsServed = snapshot.TotalRequestsServed,
            GcHeapMemoryMb = GetGcMemoryMb(),
            WorkingSetMb = GetWorkingSetMb(),
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2)
        };
    }

    internal static int GetGcMemoryMb() =>
        (int)Math.Round(GC.GetTotalMemory(false) / 1_048_576.0);

    internal static int GetWorkingSetMb() =>
        (int)Math.Round(Environment.WorkingSet / 1_048_576.0);
}
