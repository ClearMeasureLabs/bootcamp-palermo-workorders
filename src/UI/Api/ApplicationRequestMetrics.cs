namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Thread-safe in-process counter for HTTP requests.
/// </summary>
public sealed class ApplicationRequestMetrics : IApplicationRequestMetrics
{
    private long _totalRequests;

    /// <inheritdoc />
    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _totalRequests);
}
