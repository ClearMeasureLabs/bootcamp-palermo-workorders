using System.Net;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Path,
    string PathBase,
    string QueryString,
    IReadOnlyDictionary<string, string> Query,
    string Scheme,
    string Host,
    string? RemoteIpAddress,
    IReadOnlyDictionary<string, string> Headers);

/// <summary>
/// Builds <see cref="EchoResponse"/> from the incoming HTTP request, redacting sensitive headers.
/// </summary>
public static class EchoRequestReflection
{
    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Proxy-Authorization",
        "Set-Cookie",
        "X-Api-Key",
        "X-API-Key"
    };

    /// <summary>
    /// Reflects key request properties for diagnostics; never includes sensitive header values.
    /// </summary>
    public static EchoResponse Build(HttpRequest request, ConnectionInfo connection)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Query)
        {
            query[pair.Key] = pair.Value.ToString();
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (SensitiveHeaderNames.Contains(header.Key))
            {
                continue;
            }

            headers[header.Key] = header.Value.ToString();
        }

        return new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Query: query,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            RemoteIpAddress: FormatRemoteIpAddress(connection.RemoteIpAddress),
            Headers: headers);
    }

    internal static bool IsSensitiveHeader(string headerName) =>
        SensitiveHeaderNames.Contains(headerName);

    private static string? FormatRemoteIpAddress(IPAddress? address) =>
        address?.ToString();
}
