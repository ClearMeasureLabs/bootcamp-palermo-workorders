using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Assembles live runtime metrics for <see cref="MetricsSummaryResponse"/>.
/// </summary>
public static class ApplicationRuntimeMetricsBuilder
{
    /// <summary>
    /// Builds a snapshot using the current process start time.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, IRequestMetrics requestMetrics)
    {
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        return Build(timeProvider, processStartUtc, requestMetrics);
    }

    /// <summary>
    /// Builds a snapshot for a known process start instant (UTC), for tests and deterministic scenarios.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        DateTimeOffset processStartUtc,
        IRequestMetrics requestMetrics)
    {
        var health = SimpleHealthResponseBuilder.Build(timeProvider, processStartUtc);

        return new MetricsSummaryResponse(
            Uptime: health.Uptime,
            TotalRequestsServed: requestMetrics.TotalRequestsServed,
            GcMemoryMb: GetGcMemoryMb(),
            WorkingSetMb: GetWorkingSetMb(),
            GcCollectionCounts: new GcCollectionCounts(
                Gen0: GC.CollectionCount(0),
                Gen1: GC.CollectionCount(1),
                Gen2: GC.CollectionCount(2)));
    }

    internal static int GetGcMemoryMb() =>
        (int)Math.Round(GC.GetTotalMemory(false) / 1_048_576.0);

    internal static int GetWorkingSetMb() =>
        (int)Math.Round(Environment.WorkingSet / 1_048_576.0);
}
