namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Monotonic HTTP request totals for operational metrics (counts requests passing through UI.Server middleware).
/// </summary>
public interface IRequestCounters
{
    /// <summary>Total HTTP requests counted since process start.</summary>
    long TotalRequests { get; }

    /// <summary>Increases the counted request total by one (thread-safe).</summary>
    void IncrementRequest();
}
