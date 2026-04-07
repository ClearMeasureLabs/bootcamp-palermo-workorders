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
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        return Build(timeProvider, processStartUtc, totalRequestsServed);
    }

    /// <summary>
    /// Creates a snapshot for a known process start instant (UTC), for tests.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        DateTimeOffset processStartUtcUtc,
        long totalRequestsServed)
    {
        var now = timeProvider.GetUtcNow();
        var startUtc = processStartUtcUtc.ToUniversalTime();
        var uptime = now - startUtc;

        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;

        return new MetricsSummaryResponse
        {
            Uptime = uptime,
            TotalRequestsServed = totalRequestsServed,
            WorkingSetBytes = workingSet,
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2)
        };
    }
}
