using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Reflects key properties of the incoming HTTP request for debugging and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    internal const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "X-Api-Key",
        "Cookie"
    };

    /// <summary>
    /// Returns a JSON object reflecting method, path, query, host, protocol, remote IP, and headers
    /// (sensitive header values redacted).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(BuildEchoResponse(HttpContext));

    /// <summary>
    /// Builds an <see cref="EchoResponse"/> from the current HTTP context.
    /// </summary>
    private static EchoResponse BuildEchoResponse(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = SensitiveHeaderNames.Contains(header.Key)
                ? RedactedValue
                : header.Value.ToString();
        }

        return new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            QueryString: request.QueryString.HasValue ? request.QueryString.Value! : string.Empty,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Protocol: request.Protocol,
            RemoteIpAddress: FormatRemoteIp(httpContext.Connection.RemoteIpAddress),
            Headers: headers);
    }

    private static string? FormatRemoteIp(IPAddress? address) =>
        address is null ? null : address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    string Scheme,
    string Host,
    string Protocol,
    string? RemoteIpAddress,
    IReadOnlyDictionary<string, string> Headers);
