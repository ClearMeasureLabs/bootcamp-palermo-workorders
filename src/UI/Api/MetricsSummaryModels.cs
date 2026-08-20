namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    int GcMemoryMb,
    int WorkingSetMb,
    GcCollectionCounts GcCollectionCounts);

/// <summary>
/// GC collection counts by generation at the time the metrics snapshot was taken.
/// </summary>
public sealed record GcCollectionCounts(int Gen0, int Gen1, int Gen2);
