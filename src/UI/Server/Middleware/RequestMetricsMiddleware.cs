using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetricsStore"/> once per completed HTTP request.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsStore store)
{
    /// <summary>
    /// Processes the request and records it in the metrics store after the pipeline completes.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        store.Increment();
    }
}
