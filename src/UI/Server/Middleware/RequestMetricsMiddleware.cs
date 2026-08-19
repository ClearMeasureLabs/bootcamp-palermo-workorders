using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetricsCollector"/> once per HTTP request before downstream middleware runs.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Records the request and invokes the remainder of the pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IRequestMetricsCollector collector)
    {
        collector.RecordRequest();
        await next(context);
    }
}
