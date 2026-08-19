using System.Threading;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe singleton counter for HTTP requests served by the host.
/// </summary>
public sealed class RequestMetricsCounter : IRequestMetricsCounter
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void RecordRequestServed() => Interlocked.Increment(ref _totalRequestsServed);
}
