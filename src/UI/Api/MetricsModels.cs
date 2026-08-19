namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// GC collection counts by generation for <see cref="MetricsSummaryResponse"/>.
/// </summary>
public sealed record GcCollectionCounts(int Gen0, int Gen1, int Gen2);

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequests,
    int GcMemoryMb,
    int WorkingSetMb,
    GcCollectionCounts GcCollectionCounts);
