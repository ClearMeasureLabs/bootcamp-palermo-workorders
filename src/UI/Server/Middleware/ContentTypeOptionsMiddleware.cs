namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Adds <c>X-Content-Type-Options: nosniff</c> when the response does not already include that header.
/// </summary>
public sealed class ContentTypeOptionsMiddleware(RequestDelegate next)
{
    /// <summary>Header name applied to responses.</summary>
    public const string HeaderName = "X-Content-Type-Options";

    /// <summary>Header value that disables MIME sniffing.</summary>
    public const string HeaderValue = "nosniff";

    /// <summary>
    /// Invokes the next middleware after ensuring the content-type-options header is present.
    /// </summary>
    public Task InvokeAsync(HttpContext context)
    {
        ApplyHeader(context.Response.Headers);
        return next(context);
    }

    /// <summary>
    /// Sets the nosniff header when it is not already present.
    /// </summary>
    internal static void ApplyHeader(IHeaderDictionary headers)
    {
        if (!headers.ContainsKey(HeaderName))
        {
            headers[HeaderName] = HeaderValue;
        }
    }
}
