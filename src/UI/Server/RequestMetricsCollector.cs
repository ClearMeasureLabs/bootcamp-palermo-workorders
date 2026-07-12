using System.Threading;
using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe singleton request counter for <see cref="IRequestMetricsCollector"/>.
/// </summary>
public sealed class RequestMetricsCollector : IRequestMetricsCollector
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public void RecordRequest() => Interlocked.Increment(ref _totalRequestsServed);

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);
}
