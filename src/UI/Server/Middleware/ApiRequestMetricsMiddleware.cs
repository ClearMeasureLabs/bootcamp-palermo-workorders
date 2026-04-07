using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Counts completed HTTP requests on API paths for <see cref="MetricsSummaryBuilder"/>.
/// </summary>
public sealed class ApiRequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApiRequestMetricsState metricsState)
    {
        if (!ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        finally
        {
            metricsState.IncrementRequestsServed();
        }
    }
}
