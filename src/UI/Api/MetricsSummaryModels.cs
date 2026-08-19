namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    string Environment,
    TimeSpan Uptime,
    long TotalRequestsServed,
    int MemoryMb,
    GcCollectionCounts GcCollectionCounts);

/// <summary>
/// Garbage collection counts by generation for runtime metrics.
/// </summary>
public sealed record GcCollectionCounts(
    int Gen0Count,
    int Gen1Count,
    int Gen2Count);
