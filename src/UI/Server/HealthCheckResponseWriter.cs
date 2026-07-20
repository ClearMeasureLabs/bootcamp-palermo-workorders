using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Writes a concise, friendly plain-text body for the <c>/_healthcheck</c> endpoint.
/// A passing report yields a body containing "ok" alongside the overall status so
/// callers get a human-readable acknowledgement in addition to the 200 status code.
/// </summary>
public static class HealthCheckResponseWriter
{
    /// <summary>
    /// Writes the overall status of the <paramref name="report"/> to the response body.
    /// When the report is not <see cref="HealthStatus.Unhealthy"/>, the friendly text
    /// "ok" is appended so a healthy endpoint responds with e.g. "Healthy - ok".
    /// </summary>
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";

        var body = report.Status == HealthStatus.Unhealthy
            ? report.Status.ToString()
            : $"{report.Status} - ok";

        await context.Response.WriteAsync(body);
    }
}
