namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// Values are per process (not cluster-aggregated). <see cref="TotalRequestsServed"/> counts every HTTP
/// request that reached <see cref="IApplicationRuntimeMetricsSnapshot.RecordRequest"/> after routing
/// (entire app, including static files and Blazor, excluding requests that fail before the middleware runs).
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    long WorkingSetBytes,
    long GcTotalMemoryBytes,
    GcCollectionCounts GcCollections);

/// <summary>
/// GC collection counts from <see cref="GC.CollectionCount"/> for generations 0–2 at snapshot time.
/// </summary>
public sealed record GcCollectionCounts(int Gen0, int Gen1, int Gen2);
