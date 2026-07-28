namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse
{
    /// <summary>
    /// Process uptime since the current process started.
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// Total HTTP requests served by the application since startup.
    /// </summary>
    public required long TotalRequestsServed { get; init; }

    /// <summary>
    /// Current GC heap memory in megabytes (rounded).
    /// </summary>
    public required int GcHeapMemoryMb { get; init; }

    /// <summary>
    /// Current process working set in megabytes (rounded).
    /// </summary>
    public required int WorkingSetMb { get; init; }

    /// <summary>
    /// Number of generation 0 garbage collections since process start.
    /// </summary>
    public required int GcGen0Collections { get; init; }

    /// <summary>
    /// Number of generation 1 garbage collections since process start.
    /// </summary>
    public required int GcGen1Collections { get; init; }

    /// <summary>
    /// Number of generation 2 garbage collections since process start.
    /// </summary>
    public required int GcGen2Collections { get; init; }
}
