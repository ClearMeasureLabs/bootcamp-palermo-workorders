using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.ServiceDefaults;

/// <summary>
/// Ensures each HTTP request has a correlation identifier for logging, tracing, and response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const int MaxIncomingLength = 128;
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationIdConstants.HttpContextItemKey] = correlationId;
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;

        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdConstants.HeaderName, out var fromHeader))
        {
            var id = fromHeader.ToString().Trim();
            if (id.Length > 0 && id.Length <= MaxIncomingLength)
            {
                return id;
            }
        }

        return Guid.NewGuid().ToString("D");
    }
}
