namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/metrics/summary</c> and <c>GET /api/v1.0/metrics/summary</c>.
/// </summary>
public sealed record MetricsSummaryResponse
{
    /// <summary>Elapsed time since the host process started (same basis as <see cref="SimpleHealthResponseBuilder"/>).</summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>Working set size from <see cref="System.Diagnostics.Process.WorkingSet64"/>.</summary>
    public required long MemoryBytes { get; init; }

    /// <summary>Managed heap size from <see cref="GC.GetGCMemoryInfo"/>.</summary>
    public required long HeapSizeBytes { get; init; }

    /// <summary>GC collection counts for generations 0–2.</summary>
    public required GcCollectionCounts GcCollections { get; init; }

    /// <summary>Total HTTP requests counted for this process since start (see controller summary for semantics).</summary>
    public required long TotalRequestsServed { get; init; }

    /// <summary>UTC instant when the snapshot was taken.</summary>
    public required DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Per-generation GC collection counts.
/// </summary>
public sealed record GcCollectionCounts
{
    /// <summary>Collections for generation 0.</summary>
    public required int Gen0 { get; init; }

    /// <summary>Collections for generation 1.</summary>
    public required int Gen1 { get; init; }

    /// <summary>Collections for generation 2.</summary>
    public required int Gen2 { get; init; }
}
