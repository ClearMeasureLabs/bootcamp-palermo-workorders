using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryBuilder
{
    /// <summary>
    /// Creates a summary using the current process start time and live GC/memory samples.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, IRequestMetricsCounter requestCounter)
    {
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        return Build(timeProvider, processStartUtc, requestCounter);
    }

    /// <summary>
    /// Creates a summary for a known process start instant (UTC), for tests and deterministic scenarios.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        DateTimeOffset processStartUtc,
        IRequestMetricsCounter requestCounter)
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider, processStartUtc).Uptime;
        var memory = new MetricsMemorySnapshot(
            GcMemoryBytes: GC.GetTotalMemory(false),
            WorkingSetBytes: Environment.WorkingSet);
        var gcCollections = new MetricsGcCollections(
            Gen0: GC.CollectionCount(0),
            Gen1: GC.CollectionCount(1),
            Gen2: GC.CollectionCount(2));

        return new MetricsSummaryResponse(
            Uptime: uptime,
            TotalRequestsServed: requestCounter.TotalRequestsServed,
            Memory: memory,
            GcCollections: gcCollections);
    }
}
