namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Registers correlation identifier middleware on the HTTP pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that assigns or forwards <c>X-Correlation-ID</c>, adds it to logging scopes and the current <see cref="System.Diagnostics.Activity"/> when present.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<ClearMeasure.Bootcamp.ServiceDefaults.CorrelationIdMiddleware>();
}
