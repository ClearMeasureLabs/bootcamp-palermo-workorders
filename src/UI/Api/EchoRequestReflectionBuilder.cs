using ClearMeasure.Bootcamp.ServiceDefaults;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="EchoResponse"/> instances from an HTTP request for diagnostic echo endpoints.
/// </summary>
public static class EchoRequestReflectionBuilder
{
    /// <summary>
    /// Literal substituted for sensitive header values in echo responses.
    /// </summary>
    public const string RedactedHeaderValue = "[REDACTED]";

    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key"
    };

    /// <summary>
    /// Reflects key properties of <paramref name="context"/>'s HTTP request into an <see cref="EchoResponse"/>.
    /// </summary>
    public static EchoResponse Build(HttpContext context)
    {
        var request = context.Request;
        return new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value,
            QueryString: request.QueryString.Value,
            Query: CopyQuery(request.Query),
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            RemoteIp: context.Connection.RemoteIpAddress?.ToString(),
            Headers: CopyHeaders(request.Headers),
            CorrelationId: ResolveCorrelationId(context));
    }

    private static IReadOnlyDictionary<string, string[]> CopyQuery(IQueryCollection query)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
        {
            result[pair.Key] = pair.Value.Select(static v => v ?? string.Empty).ToArray();
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string[]> CopyHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in headers)
        {
            var values = SensitiveHeaderNames.Contains(pair.Key)
                ? [RedactedHeaderValue]
                : pair.Value.ToArray();
            result[pair.Key] = values;
        }

        return result;
    }

    private static string? ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdConstants.HttpContextItemKey, out var itemValue)
            && itemValue is string fromItems
            && !string.IsNullOrEmpty(fromItems))
        {
            return fromItems;
        }

        if (context.Request.Headers.TryGetValue(CorrelationIdConstants.HeaderName, out var headerValues))
        {
            var fromHeader = headerValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(fromHeader))
            {
                return fromHeader;
            }
        }

        return null;
    }
}
