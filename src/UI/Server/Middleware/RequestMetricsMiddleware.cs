using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetricsCounter"/> once per HTTP request after pipeline traversal.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsCounter counter)
{
    /// <summary>
    /// Invokes the next middleware and records the request in <c>finally</c> (including error responses).
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            counter.RecordRequestServed();
        }
    }
}
