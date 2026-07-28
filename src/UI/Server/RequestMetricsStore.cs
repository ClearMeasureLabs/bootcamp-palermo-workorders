using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe singleton implementation of <see cref="IRequestMetricsStore"/>.
/// </summary>
public sealed class RequestMetricsStore : IRequestMetricsStore
{
    private long _totalRequests;

    /// <inheritdoc />
    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _totalRequests);
}
