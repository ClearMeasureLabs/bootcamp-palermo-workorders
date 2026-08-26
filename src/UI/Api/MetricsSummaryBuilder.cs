using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryBuilder
{
    /// <summary>
    /// Creates a summary using the process start time from <see cref="Process.GetCurrentProcess"/>.
    /// </summary>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, IHttpRequestMetricsCounter requestCounter) =>
        Build(timeProvider, requestCounter, new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime());

    /// <summary>
    /// Creates a summary for a known process start instant (UTC), for tests and deterministic scenarios.
    /// </summary>
    public static MetricsSummaryResponse Build(
        TimeProvider timeProvider,
        IHttpRequestMetricsCounter requestCounter,
        DateTimeOffset processStartUtc)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(requestCounter);

        var now = timeProvider.GetUtcNow();
        var startUtc = processStartUtc.ToUniversalTime();
        var uptime = now - startUtc;

        return new MetricsSummaryResponse(
            Uptime: uptime,
            TotalRequestsServed: requestCounter.Total,
            WorkingSetBytes: Environment.WorkingSet,
            ManagedMemoryBytes: GC.GetTotalMemory(false),
            GcGen0Collections: GC.CollectionCount(0),
            GcGen1Collections: GC.CollectionCount(1),
            GcGen2Collections: GC.CollectionCount(2));
    }
}
