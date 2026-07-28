using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON reflection of the incoming HTTP request for client and operator debugging.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns key properties of the current HTTP request (method, path, query, headers, host, client IP).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var request = Request;
        var headers = request.Headers
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray()), StringComparer.OrdinalIgnoreCase);
        var query = request.Query
            .ToDictionary(
                q => q.Key,
                q => q.Value.Select(v => v ?? string.Empty).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var payload = new EchoResponse(
            Method: request.Method,
            Path: request.Path.Value ?? string.Empty,
            QueryString: request.QueryString.Value ?? string.Empty,
            Scheme: request.Scheme,
            Host: request.Host.Value ?? string.Empty,
            Protocol: request.Protocol,
            RemoteIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
            Headers: headers,
            Query: query);

        return ConditionalGetEtag.JsonContent(payload);
    }
}
