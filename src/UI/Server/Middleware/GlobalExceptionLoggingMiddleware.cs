using System.Diagnostics;
using ClearMeasure.Bootcamp.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Logs unhandled exceptions on the originating pipeline pass before the exception handler writes the response.
/// Must run inside the same <see cref="Microsoft.AspNetCore.Builder.ExceptionHandlerExtensions.UseExceptionHandler"/> branch as downstream endpoints so exceptions are observed; re-throws to preserve existing problem-details behavior.
/// </summary>
public sealed class GlobalExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionLoggingMiddleware> _logger;

    public GlobalExceptionLoggingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var activityId = Activity.Current?.Id;
            string? correlationId = null;
            if (context.Items.TryGetValue(CorrelationIdConstants.HttpContextItemKey, out var corrItem)
                && corrItem is string corr)
            {
                correlationId = corr;
            }

            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path} TraceIdentifier {TraceIdentifier} ActivityId {ActivityId} CorrelationId {CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier,
                activityId,
                correlationId);
            throw;
        }
    }
}
