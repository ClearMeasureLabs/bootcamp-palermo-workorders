using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="EchoResponse"/> from the current HTTP request for diagnostic echo endpoints.
/// </summary>
public static class EchoRequestReflection
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> DiagnosticHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "User-Agent",
        "Host",
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Correlation-ID",
        "Content-Type"
    };

    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "X-API-Key",
        "Cookie"
    };

    /// <summary>
    /// Reflects key properties of <paramref name="request"/> into an <see cref="EchoResponse"/>.
    /// </summary>
    public static EchoResponse Build(HttpRequest request, ConnectionInfo connection)
    {
        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(v => v ?? string.Empty).ToArray(),
            StringComparer.OrdinalIgnoreCase);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (SensitiveHeaderNames.Contains(header.Key))
            {
                headers[header.Key] = RedactedValue;
                continue;
            }

            if (DiagnosticHeaderNames.Contains(header.Key))
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        return new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Query: query,
            Headers: headers,
            ClientIp: ResolveClientIp(request, connection));
    }

    internal static string? ResolveClientIp(HttpRequest request, ConnectionInfo connection)
    {
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstHop = forwardedFor.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstHop))
            {
                return firstHop;
            }
        }

        return connection.RemoteIpAddress?.ToString();
    }
}
