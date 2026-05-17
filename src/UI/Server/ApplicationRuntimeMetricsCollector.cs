using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe per-process HTTP request counter and runtime metrics snapshot for the metrics summary API.
/// </summary>
public sealed class ApplicationRuntimeMetricsCollector : IApplicationRuntimeMetricsSnapshot
{
    private long _totalRequests;

    /// <inheritdoc />
    public void RecordRequest() => Interlocked.Increment(ref _totalRequests);

    /// <inheritdoc />
    public MetricsSummaryResponse Build(TimeProvider timeProvider)
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider).Uptime;
        var workingSet = Process.GetCurrentProcess().WorkingSet64;
        var gcMemory = GC.GetTotalMemory(false);
        var gc = new GcCollectionCounts(
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
        return new MetricsSummaryResponse(
            uptime,
            Interlocked.Read(ref _totalRequests),
            workingSet,
            gcMemory,
            gc);
    }
}
