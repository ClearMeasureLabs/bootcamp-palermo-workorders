namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and the versioned path.
/// </summary>
public sealed class MetricsSummaryResponse
{
    /// <summary>Elapsed time since the host process started.</summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>HTTP requests completed through the API rate-limiting surface since process start.</summary>
    public required long TotalRequestsServed { get; init; }

    /// <summary>Process working set size in bytes.</summary>
    public required long WorkingSetBytes { get; init; }

    /// <summary>GC collection counts for generation 0 since process start.</summary>
    public required int GcGen0Collections { get; init; }

    /// <summary>GC collection counts for generation 1 since process start.</summary>
    public required int GcGen1Collections { get; init; }

    /// <summary>GC collection counts for generation 2 since process start.</summary>
    public required int GcGen2Collections { get; init; }
}
