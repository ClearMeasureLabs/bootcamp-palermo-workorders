namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c>.
/// </summary>
public record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    MetricsMemorySnapshot Memory,
    MetricsGcCollections GcCollections);

/// <summary>
/// Process memory snapshot in bytes.
/// </summary>
public record MetricsMemorySnapshot(long GcMemoryBytes, long WorkingSetBytes);

/// <summary>
/// GC collection counts for generations 0–2.
/// </summary>
public record MetricsGcCollections(int Gen0, int Gen1, int Gen2);
