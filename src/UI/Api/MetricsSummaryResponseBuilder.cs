using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryResponseBuilder
{
    /// <summary>
    /// Creates a response using the process start time from <see cref="Process.GetCurrentProcess"/>.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, long totalRequestsServed)
    {
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        return Build(timeProvider, totalRequestsServed, processStartUtc);
    }

    /// <summary>
    /// Creates a response for a known process start instant (UTC), for tests and deterministic scenarios.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        long totalRequestsServed,
        DateTimeOffset processStartUtcUtc)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider, processStartUtcUtc);
        var memoryBytes = GC.GetTotalMemory(forceFullCollection: false);
        var gcCollections = new GcCollectionCounts(
            Gen0: GC.CollectionCount(0),
            Gen1: GC.CollectionCount(1),
            Gen2: GC.CollectionCount(2));

        return new MetricsSummaryResponse(
            Uptime: healthSlice.Uptime,
            TotalRequestsServed: totalRequestsServed,
            CurrentMemoryBytes: memoryBytes,
            GcCollections: gcCollections);
    }
}
