namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Stable operational JSON payload for <c>GET /api/metrics/summary</c>. Property names are a public monitoring contract only for this host process.
/// </summary>
/// <remarks>
/// <para>
/// Counts reflect the web host only (typically UI.Server); request totals include every HTTP hit recorded by middleware (static assets, APIs, SignalR negotiation, etc.), not aggregates across Worker or MCP processes.
/// </para>
/// </remarks>
/// <param name="Uptime">Elapsed time since the host process started (same source as diagnostics/health).</param>
/// <param name="TotalRequests">Monotonic count of HTTP requests through the server's counting middleware.</param>
/// <param name="ManagedMemoryBytes">Managed heap approximation in bytes (<see cref="GC.GetTotalMemory(bool)"/> with <c>false</c>).</param>
/// <param name="GcGen0Collections"><see cref="GC.CollectionCount(int)"/> generation 0 at response time.</param>
/// <param name="GcGen1Collections"><see cref="GC.CollectionCount(int)"/> generation 1 at response time.</param>
/// <param name="GcGen2Collections"><see cref="GC.CollectionCount(int)"/> generation 2 at response time.</param>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequests,
    long ManagedMemoryBytes,
    int GcGen0Collections,
    int GcGen1Collections,
    int GcGen2Collections);
