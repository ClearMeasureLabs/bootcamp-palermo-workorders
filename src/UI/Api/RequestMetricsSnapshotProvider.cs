namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Thread-safe, process-local request counter and metrics snapshot provider.
/// </summary>
public sealed class RequestMetricsSnapshotProvider : IRequestMetricsSnapshot
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void RecordRequest() =>
        Interlocked.Increment(ref _totalRequestsServed);

    /// <inheritdoc />
    public MetricsSummaryResponse BuildSummary(TimeProvider timeProvider)
    {
        var healthSlice = SimpleHealthResponseBuilder.Build(timeProvider);
        return new MetricsSummaryResponse(
            Uptime: healthSlice.Uptime,
            TotalRequestsServed: TotalRequestsServed,
            WorkingSetMb: ProcessMemoryMetrics.GetWorkingSetMb(),
            GcMemoryMb: ProcessMemoryMetrics.GetGcMemoryMb(),
            GcCollectionCounts: new GcCollectionCounts(
                Gen0: GC.CollectionCount(0),
                Gen1: GC.CollectionCount(1),
                Gen2: GC.CollectionCount(2)),
            CapturedAtUtc: timeProvider.GetUtcNow().UtcDateTime);
    }
}
