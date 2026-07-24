using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IApplicationRequestMetrics"/> for every HTTP request.
/// </summary>
public sealed class ApplicationRequestMetricsMiddleware(
    RequestDelegate next,
    IApplicationRequestMetrics requestMetrics)
{
    /// <summary>
    /// Counts the request and invokes the remainder of the pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        requestMetrics.Increment();
        await next(context);
    }
}
