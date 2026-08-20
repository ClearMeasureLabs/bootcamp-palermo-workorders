using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetricsCounter"/> once per completed HTTP request.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsCounter requestMetricsCounter)
{
    /// <summary>
    /// Invokes the next middleware, then records the request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            requestMetricsCounter.RecordRequest();
        }
    }
}
