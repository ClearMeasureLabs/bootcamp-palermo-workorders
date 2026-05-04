using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IHttpRequestMetrics"/> for every HTTP request that passes through the pipeline after this middleware runs.
/// </summary>
public sealed class HttpRequestCountingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the next delegate in the pipeline after recording the request.
    /// </summary>
    public Task InvokeAsync(HttpContext context, IHttpRequestMetrics metrics)
    {
        metrics.RecordRequest();
        return next(context);
    }
}
