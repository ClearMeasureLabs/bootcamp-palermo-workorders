using System.Threading;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe implementation of <see cref="IHttpRequestMetrics"/>.
/// </summary>
public sealed class HttpRequestMetrics : IHttpRequestMetrics
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void RecordRequest() => Interlocked.Increment(ref _totalRequestsServed);
}
