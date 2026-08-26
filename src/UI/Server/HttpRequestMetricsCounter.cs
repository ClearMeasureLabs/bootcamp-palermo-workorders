using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe in-process counter of HTTP requests served since process start.
/// </summary>
public sealed class HttpRequestMetricsCounter : IHttpRequestMetricsCounter
{
    private long _total;

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _total);

    /// <inheritdoc />
    public long Total => Interlocked.Read(ref _total);
}
