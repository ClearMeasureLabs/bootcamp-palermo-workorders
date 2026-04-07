using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns a JSON snapshot of the inbound HTTP request for debugging.
/// Secret-bearing headers (<c>Authorization</c>, <c>Cookie</c>, <c>X-Api-Key</c>) are not echoed in clear text; values are replaced with <see cref="EchoResponse.RedactedPlaceholder"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class EchoController : ControllerBase
{
    private const int MaxHeadersReturned = 64;

    private static readonly HashSet<string> SensitiveHeaderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Authorization,
            HeaderNames.Cookie,
            "X-Api-Key"
        };

    private static readonly HashSet<string> HopByHopHeaderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Connection,
            HeaderNames.KeepAlive,
            "Proxy-Authenticate",
            "Proxy-Authorization",
            HeaderNames.TE,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade
        };

    /// <summary>
    /// Reflects method, URL parts, connection metadata, and non-secret headers (capped, hop-by-hop excluded).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var req = HttpContext.Request;
        var conn = HttpContext.Connection;

        var url = $"{req.Scheme}://{req.Host}{req.PathBase}{req.Path}{req.QueryString}";

        var headerEntries = new List<EchoHeaderEntry>();
        foreach (var header in req.Headers.OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (HopByHopHeaderNames.Contains(header.Key))
            {
                continue;
            }

            var value = SensitiveHeaderNames.Contains(header.Key)
                ? EchoResponse.RedactedPlaceholder
                : string.Join(", ", header.Value.AsEnumerable());
            headerEntries.Add(new EchoHeaderEntry(header.Key, value));
            if (headerEntries.Count >= MaxHeadersReturned)
            {
                break;
            }
        }

        var payload = new EchoResponse(
            Method: req.Method,
            Scheme: req.Scheme,
            Host: req.Host.Value ?? string.Empty,
            PathBase: req.PathBase.Value ?? string.Empty,
            Path: req.Path.Value ?? string.Empty,
            QueryString: req.QueryString.Value ?? string.Empty,
            Url: url,
            RemoteIpAddress: conn.RemoteIpAddress?.ToString(),
            RemotePort: conn.RemotePort is > 0 ? conn.RemotePort : null,
            Headers: headerEntries);

        return Ok(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
/// <param name="Headers">Subset of request headers; secret values are replaced with <see cref="RedactedPlaceholder"/>.</param>
public sealed record EchoResponse(
    string Method,
    string Scheme,
    string Host,
    string PathBase,
    string Path,
    string QueryString,
    string Url,
    string? RemoteIpAddress,
    int? RemotePort,
    IReadOnlyList<EchoHeaderEntry> Headers)
{
    /// <summary>
    /// Serialized in place of sensitive header values.
    /// </summary>
    public const string RedactedPlaceholder = "[REDACTED]";
}

/// <summary>
/// One reflected header name and value for <see cref="EchoResponse"/>.
/// </summary>
public sealed record EchoHeaderEntry(string Name, string Value);
