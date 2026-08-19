namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    int WorkingSetMb,
    int GcMemoryMb,
    GcCollectionCounts GcCollectionCounts,
    DateTime CapturedAtUtc);

/// <summary>
/// Garbage-collection collection counts per generation at snapshot time.
/// </summary>
public sealed record GcCollectionCounts(int Gen0, int Gen1, int Gen2);
