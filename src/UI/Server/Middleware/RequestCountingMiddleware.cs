using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments the process-wide HTTP request counter for every inbound Kestrel request.
/// </summary>
public sealed class RequestCountingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Records the request and invokes the remainder of the pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IRequestMetricsSnapshot requestMetricsSnapshot)
    {
        requestMetricsSnapshot.RecordRequest();
        await next(context);
    }
}
