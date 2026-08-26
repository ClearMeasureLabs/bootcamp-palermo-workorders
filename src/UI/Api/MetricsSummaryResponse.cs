namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    long WorkingSetBytes,
    long ManagedMemoryBytes,
    int GcGen0Collections,
    int GcGen1Collections,
    int GcGen2Collections);
