namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks HTTP requests completed on API paths (same scope as server-side rate limiting path matching).
/// </summary>
public sealed class ApiRequestMetricsState
{
    private long _totalRequestsServed;

    /// <summary>Requests that finished the pipeline after matching the counted path prefix.</summary>
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <summary>Increments the served counter (thread-safe).</summary>
    public void IncrementRequestsServed() => Interlocked.Increment(ref _totalRequestsServed);
}
