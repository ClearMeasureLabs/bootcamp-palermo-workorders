namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Thread-safe in-process request counter.
/// </summary>
public sealed class RequestMetrics : IRequestMetrics
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _totalRequestsServed);
}
