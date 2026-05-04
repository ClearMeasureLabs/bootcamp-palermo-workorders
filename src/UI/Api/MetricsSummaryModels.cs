namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
/// <param name="Uptime">Elapsed time since the host process started (same semantics as <see cref="SimpleHealthResponseBuilder"/>).</param>
/// <param name="TotalRequestsServed">Requests counted by host middleware since process start; not sampled Application Insights telemetry.</param>
/// <param name="WorkingSetBytes">Current process working set from <see cref="System.Diagnostics.Process.WorkingSet64"/>.</param>
/// <param name="TotalAllocatedBytes">Bytes allocated on the managed heap since the process started (<see cref="GC.GetTotalAllocatedBytes"/>).</param>
/// <param name="GcGen0Collections">Gen 0 GC collection count.</param>
/// <param name="GcGen1Collections">Gen 1 GC collection count.</param>
/// <param name="GcGen2Collections">Gen 2 GC collection count.</param>
public sealed record MetricsSummaryResponse(
    TimeSpan Uptime,
    long TotalRequestsServed,
    long WorkingSetBytes,
    long TotalAllocatedBytes,
    int GcGen0Collections,
    int GcGen1Collections,
    int GcGen2Collections);
