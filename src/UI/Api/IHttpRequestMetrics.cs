namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Process-wide count of HTTP requests observed by host middleware after routing.
/// Increments once per request that reaches the counter (Kestrel HTTP pipeline); excludes requests that never reach the middleware (for example some gRPC paths if not routed through the same branch).
/// </summary>
public interface IHttpRequestMetrics
{
    /// <summary>Total requests recorded since process start.</summary>
    long TotalRequestsServed { get; }

    /// <summary>Records one request (thread-safe).</summary>
    void RecordRequest();
}
