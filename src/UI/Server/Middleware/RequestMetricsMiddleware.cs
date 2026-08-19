using ClearMeasure.Bootcamp.UI.Api;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Increments <see cref="IRuntimeMetricsCollector"/> for each HTTP request handled by the pipeline.
/// </summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, IRuntimeMetricsCollector metricsCollector)
    {
        await next(context);
        metricsCollector.RecordRequest();
    }
}
