namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Process-lifetime counter of inbound HTTP requests, updated by <see cref="ClearMeasure.Bootcamp.UI.Server.Middleware.RequestMetricsMiddleware"/>.
/// </summary>
public interface IRequestMetricsStore
{
    /// <summary>
    /// Total requests observed since process start.
    /// </summary>
    long TotalRequests { get; }

    /// <summary>
    /// Records one completed HTTP request.
    /// </summary>
    void Increment();
}
