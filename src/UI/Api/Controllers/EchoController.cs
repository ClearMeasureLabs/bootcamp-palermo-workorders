using System.Collections.Frozen;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Reflects selected properties of the incoming HTTP request for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class EchoController : ControllerBase
{
    private const int MaxHeaderValueLength = 4096;

    private static readonly FrozenSet<string> SensitiveHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Proxy-Authorization",
        "Set-Cookie",
        "X-Api-Key",
        "X-API-Key"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns JSON describing how the server observed this GET request (method, URL parts, safe headers).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        Response.Headers.Append("Cache-Control", "no-store");

        var request = HttpContext.Request;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (SensitiveHeaderNames.Contains(header.Key))
            {
                if (header.Value.Count > 0)
                    headers[header.Key] = "***";
                continue;
            }

            var combined = string.Join(", ", header.Value.AsEnumerable());
            headers[header.Key] = TruncateIfNeeded(combined);
        }

        var dto = new EchoResponse(
            Method: request.Method,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Path: request.Path.Value ?? string.Empty,
            PathBase: request.PathBase.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Headers: headers);

        return Ok(dto);
    }

    private static string TruncateIfNeeded(string value)
    {
        if (value.Length <= MaxHeaderValueLength)
            return value;
        return string.Concat(value.AsSpan(0, MaxHeaderValueLength - 3), "...");
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Scheme,
    string Host,
    string Path,
    string PathBase,
    string QueryString,
    IReadOnlyDictionary<string, string> Headers);
