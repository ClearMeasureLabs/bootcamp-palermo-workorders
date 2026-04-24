using System.Net;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Api;

// Must match ClearMeasure.Bootcamp.UI.Shared.ApiKeyConstants.HeaderName (UI.Api does not reference UI.Shared).
internal static class EchoApiKeyHeaderName
{
    internal const string Value = "X-Api-Key";
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>, reflecting non-sensitive request metadata for debugging.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Scheme,
    string Host,
    string PathBase,
    string Path,
    string FullPath,
    IReadOnlyList<KeyValuePair<string, string>> Query,
    IReadOnlyDictionary<string, string> Headers,
    EchoSensitiveHeadersPresent SensitiveHeadersPresent,
    EchoConnectionMetadata Connection);

/// <summary>
/// Indicates whether sensitive headers were present on the request (values are never echoed).
/// </summary>
public sealed record EchoSensitiveHeadersPresent(
    bool Authorization,
    bool Cookie,
    bool ApiKey,
    bool ProxyAuthorization);

/// <summary>
/// Client and server socket metadata for the current request.
/// </summary>
public sealed record EchoConnectionMetadata(
    string? RemoteIpAddress,
    int? RemotePort,
    string? LocalIpAddress,
    int? LocalPort);

/// <summary>
/// Builds <see cref="EchoResponse"/> from an <see cref="HttpContext"/> using header allowlists and secret redaction rules.
/// </summary>
public static class EchoResponseBuilder
{
    private static readonly HashSet<string> SafeHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "User-Agent",
        "Accept",
        "Accept-Language",
        "Accept-Encoding",
        "Referer",
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Forwarded-Host",
        "X-Request-Id",
        "X-Correlation-Id",
        "Traceparent",
        "Tracestate",
        "X-Test-Echo"
    };

    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        EchoApiKeyHeaderName.Value,
        "Proxy-Authorization"
    };

    private static readonly HashSet<string> SensitiveQueryKeySubstrings = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "client_secret",
        "authorization"
    };

    private const int MaxQueryPairs = 64;
    private const int MaxHeaderEntries = 32;

    /// <summary>
    /// Creates an echo snapshot from <paramref name="context"/>.
    /// </summary>
    public static EchoResponse Build(HttpContext context)
    {
        var request = context.Request;
        var connection = context.Connection;

        var queryPairs = BuildQueryPairs(request.Query);
        var headers = BuildSafeHeaders(request.Headers);

        var sensitive = new EchoSensitiveHeadersPresent(
            Authorization: request.Headers.ContainsKey("Authorization"),
            Cookie: request.Headers.ContainsKey("Cookie"),
            ApiKey: request.Headers.ContainsKey(EchoApiKeyHeaderName.Value),
            ProxyAuthorization: request.Headers.ContainsKey("Proxy-Authorization"));

        var conn = new EchoConnectionMetadata(
            FormatIp(connection.RemoteIpAddress),
            connection.RemotePort is > 0 ? connection.RemotePort : null,
            FormatIp(connection.LocalIpAddress),
            connection.LocalPort is > 0 ? connection.LocalPort : null);

        var pathBase = request.PathBase.HasValue ? request.PathBase.Value! : string.Empty;
        var path = request.Path.HasValue ? request.Path.Value! : string.Empty;
        var fullPath = pathBase + path;

        return new EchoResponse(
            Method: request.Method,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            PathBase: pathBase,
            Path: path,
            FullPath: fullPath,
            Query: queryPairs,
            Headers: headers,
            SensitiveHeadersPresent: sensitive,
            Connection: conn);
    }

    private static IReadOnlyList<KeyValuePair<string, string>> BuildQueryPairs(IQueryCollection query)
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var pair in query)
        {
            if (list.Count >= MaxQueryPairs)
                break;

            var key = pair.Key ?? string.Empty;
            var raw = pair.Value.Count > 0 ? pair.Value.ToString() : string.Empty;
            var value = IsSensitiveQueryKey(key) ? "[redacted]" : raw;
            list.Add(new KeyValuePair<string, string>(key, value));
        }

        return list;
    }

    private static bool IsSensitiveQueryKey(string key)
    {
        foreach (var fragment in SensitiveQueryKeySubstrings)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IReadOnlyDictionary<string, string> BuildSafeHeaders(IHeaderDictionary requestHeaders)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in requestHeaders)
        {
            if (dict.Count >= MaxHeaderEntries)
                break;

            if (!SafeHeaderNames.Contains(header.Key))
                continue;

            if (SensitiveHeaderNames.Contains(header.Key))
                continue;

            dict[header.Key] = header.Value.ToString();
        }

        return dict;
    }

    private static string? FormatIp(IPAddress? address) => address?.ToString();
}
