namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Singleton <see cref="IRequestCounters"/> using <see cref="Interlocked"/> increments.
/// </summary>
public sealed class RequestCounters : IRequestCounters
{
    private long _totalRequests;

    /// <inheritdoc />
    public long TotalRequests => Volatile.Read(ref _totalRequests);

    /// <inheritdoc />
    public void IncrementRequest() => Interlocked.Increment(ref _totalRequests);
}
