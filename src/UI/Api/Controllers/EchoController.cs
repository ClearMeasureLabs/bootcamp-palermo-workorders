using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns a JSON snapshot of the inbound GET request for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    private static readonly HashSet<string> AllowedHeaderNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Accept",
            "Accept-Encoding",
            "Accept-Language",
            "User-Agent",
            "Host",
            "Referer",
            "X-Correlation-ID",
            "X-Request-ID",
            "traceparent"
        };

    /// <summary>
    /// Returns JSON reflecting method, URL parts, remote IP, protocol, parsed query (last value wins per key),
    /// and allowlisted headers only.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query)
        {
            var values = pair.Value;
            query[pair.Key] = values.Count > 0 ? values[^1]! : string.Empty;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Headers)
        {
            if (!AllowedHeaderNames.Contains(pair.Key))
            {
                continue;
            }

            headers[pair.Key] = pair.Value.Count > 0 ? pair.Value.ToString() : string.Empty;
        }

        var remote = HttpContext.Connection.RemoteIpAddress;
        var remoteText = remote?.ToString();

        var payload = new EchoResponse(
            Method: Request.Method,
            Scheme: Request.Scheme,
            Host: Request.Host.HasValue ? Request.Host.Value : string.Empty,
            Path: Request.Path.Value ?? string.Empty,
            PathBase: Request.PathBase.Value ?? string.Empty,
            QueryString: Request.QueryString.HasValue ? Request.QueryString.Value! : string.Empty,
            Query: query,
            RemoteIpAddress: remoteText,
            Protocol: Request.Protocol,
            Headers: headers);

        return ConditionalGetEtag.JsonContent(payload);
    }
}
