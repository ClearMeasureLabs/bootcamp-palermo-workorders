using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="MetricsSummaryResponse"/> for <c>GET /api/metrics/summary</c>.
/// </summary>
public static class MetricsSummaryResponseBuilder
{
    /// <summary>
    /// Creates a runtime metrics snapshot using the supplied clock and request counter value.
    /// </summary>
    /// <param name="timeProvider">Clock for uptime calculation.</param>
    /// <param name="totalRequestsServed">Total requests served (injected for deterministic tests).</param>
    public static MetricsSummaryResponse Build(TimeProvider timeProvider, long totalRequestsServed)
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider).Uptime;
        var managedMemoryBytes = GC.GetTotalMemory(false);
        var workingSetBytes = Process.GetCurrentProcess().WorkingSet64;
        var gcCollectionCounts = new GcCollectionCounts(
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));

        return new MetricsSummaryResponse(
            uptime,
            totalRequestsServed,
            managedMemoryBytes,
            workingSetBytes,
            gcCollectionCounts);
    }
}
