namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
/// <param name="Uptime">Process uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>).</param>
/// <param name="TotalRequestsServed">
/// Total HTTP requests observed by the host since process start (all traffic passing request-metrics middleware, not API-only).
/// </param>
/// <param name="ManagedMemoryBytes">Managed heap size from <see cref="GC.GetTotalMemory(bool)"/> without forcing collection.</param>
/// <param name="WorkingSetBytes">Process working set from <see cref="System.Diagnostics.Process.WorkingSet64"/>.</param>
/// <param name="GcCollectionCounts">GC collection counts per generation.</param>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    long ManagedMemoryBytes,
    long WorkingSetBytes,
    GcCollectionCounts GcCollectionCounts);

/// <summary>
/// GC collection counts for generations 0, 1, and 2.
/// </summary>
/// <param name="Gen0">Collections for generation 0.</param>
/// <param name="Gen1">Collections for generation 1.</param>
/// <param name="Gen2">Collections for generation 2.</param>
public sealed record GcCollectionCounts(int Gen0, int Gen1, int Gen2);
