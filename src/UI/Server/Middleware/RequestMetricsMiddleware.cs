using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetrics"/> for every HTTP request that passes through the pipeline.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, IRequestMetrics requestMetrics)
{
    /// <summary>
    /// Records the request and invokes the remainder of the pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        requestMetrics.Increment();
        await next(context);
    }
}
