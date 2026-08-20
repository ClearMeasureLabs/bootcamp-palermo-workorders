using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes request reflection for operators and client diagnostics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/echo")]
[Route($"{ApiRoutes.VersionedApiPrefix}/echo")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Returns JSON reflecting method, path, query, headers, and client IP of the incoming request.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var path = Request.PathBase.HasValue
            ? Request.PathBase + Request.Path
            : Request.Path;

        var query = Request.Query.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(v => v ?? string.Empty).ToArray());

        var headers = Request.Headers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(v => v ?? string.Empty).ToArray());

        var payload = new EchoResponse(
            Method: Request.Method,
            Path: path.Value ?? string.Empty,
            Query: query,
            Headers: headers,
            ClientIp: HttpContext.Connection.RemoteIpAddress?.ToString());

        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public record EchoResponse(
    string Method,
    string Path,
    Dictionary<string, string[]> Query,
    Dictionary<string, string[]> Headers,
    string? ClientIp);
