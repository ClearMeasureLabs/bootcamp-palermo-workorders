using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(BuildEchoResponse(HttpContext));

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

public record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    string Scheme,
    string Host,
    string Protocol,
    string? RemoteIpAddress,
    IReadOnlyDictionary<string, string> Headers);
