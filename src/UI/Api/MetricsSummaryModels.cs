namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    long CurrentMemoryBytes,
    GcCollectionCounts GcCollections);

/// <summary>
/// Process-lifetime GC collection counts by generation (informational; reset on process restart).
/// </summary>
public sealed record GcCollectionCounts(
    int Gen0,
    int Gen1,
    int Gen2);
