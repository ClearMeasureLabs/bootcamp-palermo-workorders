using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Counts incoming HTTP requests for <see cref="MetricsSummaryController"/> (whole pipeline: static assets, APIs, etc.).
/// </summary>
internal sealed class RequestCountingMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext httpContext)
    {
        var counters = httpContext.RequestServices.GetRequiredService<IRequestCounters>();
        counters.IncrementRequest();
        return next(httpContext);
    }
}
