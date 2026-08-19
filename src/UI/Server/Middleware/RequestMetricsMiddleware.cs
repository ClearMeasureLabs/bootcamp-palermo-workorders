using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetricsCollector"/> once per HTTP request that reaches the middleware.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMetricsCollector _collector;

    public RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsCollector collector)
    {
        _next = next;
        _collector = collector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _collector.RecordRequest();
        await _next(context);
    }
}
