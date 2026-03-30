using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Logs each HTTP request with method, path, status code, and elapsed duration.
/// </summary>
internal sealed class HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
{
    private const string MessageTemplate =
        "HTTP {Method} {Path} responded {StatusCode} in {DurationMs} ms";

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
            logger.LogInformation(
                MessageTemplate,
                context.Request.Method,
                path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
