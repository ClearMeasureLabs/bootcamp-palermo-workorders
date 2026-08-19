using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetrics.TotalRequestsServed"/> for each HTTP request handled by the pipeline.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRequestMetrics requestMetrics)
    {
        requestMetrics.IncrementTotalRequestsServed();
        await next(context);
    }
}
