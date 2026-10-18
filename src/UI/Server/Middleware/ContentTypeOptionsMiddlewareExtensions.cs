namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Registers <see cref="ContentTypeOptionsMiddleware"/> in the HTTP pipeline.
/// </summary>
public static class ContentTypeOptionsMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that sets <c>X-Content-Type-Options: nosniff</c> when absent.
    /// </summary>
    public static IApplicationBuilder UseContentTypeOptions(this IApplicationBuilder app) =>
        app.UseMiddleware<ContentTypeOptionsMiddleware>();
}
