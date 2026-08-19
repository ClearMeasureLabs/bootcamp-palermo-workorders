using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe implementation of <see cref="IRequestMetrics"/>.
/// </summary>
public sealed class RequestMetrics : IRequestMetrics
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void IncrementTotalRequestsServed() =>
        Interlocked.Increment(ref _totalRequestsServed);
}
