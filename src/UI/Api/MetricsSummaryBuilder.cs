using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for the metrics summary endpoint.
/// </summary>
public static class MetricsSummaryBuilder
{
    /// <summary>
    /// Creates a snapshot using the current process and <paramref name="totalRequestsServed"/>.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, long totalRequestsServed)
    {
        using var process = Process.GetCurrentProcess();
        var processStartUtc = new DateTimeOffset(process.StartTime).ToUniversalTime();
        return Build(timeProvider, processStartUtc, process.WorkingSet64, totalRequestsServed);
    }

    /// <summary>
    /// Creates a snapshot for a known process start instant and working set, for tests.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        DateTimeOffset processStartUtc,
        long workingSetBytes,
        long totalRequestsServed)
    {
        var now = timeProvider.GetUtcNow();
        var startUtc = processStartUtc.ToUniversalTime();
        var uptime = now - startUtc;

        return new MetricsSummaryResponse
        {
            Uptime = uptime,
            TotalRequestsServed = totalRequestsServed,
            WorkingSetBytes = workingSetBytes,
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2)
        };
    }
}
