using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IHttpRequestMetricsCounter"/> once per HTTP request.
/// </summary>
public sealed class HttpRequestMetricsMiddleware(RequestDelegate next, IHttpRequestMetricsCounter counter)
{
    /// <summary>
    /// Invokes the next middleware after recording the request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        counter.Increment();
        await next(context);
    }
}
