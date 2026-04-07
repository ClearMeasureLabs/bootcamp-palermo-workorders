using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRequestMetrics"/> once per incoming HTTP request.
/// </summary>
public sealed class RequestCountingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the next middleware after recording the request.
    /// </summary>
    public Task InvokeAsync(HttpContext context, IRequestMetrics requestMetrics)
    {
        requestMetrics.Increment();
        return next(context);
    }
}
