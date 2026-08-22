using System.Threading;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe process-lifetime counter of completed HTTP requests.
/// </summary>
public sealed class RequestMetricsCounter : IRequestMetricsCounter
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <inheritdoc />
    public void RecordRequest() => Interlocked.Increment(ref _totalRequestsServed);
}
