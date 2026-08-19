using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Singleton collector for HTTP request counts and runtime metrics summary assembly.
/// </summary>
public sealed class RuntimeMetricsCollector : IRuntimeMetricsCollector
{
    private long _totalRequests;

    /// <inheritdoc />
    public void RecordRequest() => Interlocked.Increment(ref _totalRequests);

    /// <inheritdoc />
    public MetricsSummaryResponse BuildSummary(TimeProvider timeProvider)
    {
        var uptime = SimpleHealthResponseBuilder.Build(timeProvider).Uptime;
        return new MetricsSummaryResponse(
            Uptime: uptime,
            TotalRequests: Interlocked.Read(ref _totalRequests),
            GcMemoryMb: DetailedHealthReportProvider.GetGcMemoryMb(),
            WorkingSetMb: DetailedHealthReportProvider.GetWorkingSetMb(),
            GcCollectionCounts: new GcCollectionCounts(
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2)));
    }
}
