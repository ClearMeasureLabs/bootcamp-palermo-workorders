using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Thread-safe in-process counter for total HTTP requests entering the pipeline.
/// </summary>
public sealed class RequestMetricsCollector : IRequestMetricsSnapshot
{
    private long _totalRequestsServed;

    /// <inheritdoc />
    public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);

    /// <summary>
    /// Records one completed HTTP request.
    /// </summary>
    public void RecordRequest() => Interlocked.Increment(ref _totalRequestsServed);
}
