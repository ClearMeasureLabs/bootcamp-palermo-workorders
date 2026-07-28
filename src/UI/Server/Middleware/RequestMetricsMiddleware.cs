using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments the in-process request counter for every HTTP request entering the pipeline.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, RequestMetricsCollector collector)
{
    public async Task InvokeAsync(HttpContext context)
    {
        collector.RecordRequest();
        await next(context);
    }
}
