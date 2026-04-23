using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments the application-wide HTTP request counter once per request after routing.
/// </summary>
public sealed class HttpRequestMetricsMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IApplicationRuntimeMetricsSnapshot metrics)
    {
        metrics.RecordRequest();
        return next(context);
    }
}
