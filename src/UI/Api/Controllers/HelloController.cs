using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal JSON greeting for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HelloController : ControllerBase
{
    private static readonly HelloMessageResponse Payload = new("Hello, World!");

    /// <summary>
    /// Returns a static JSON greeting with HTTP 200.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(Payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(Payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/hello</c> and <c>GET /api/v1.0/hello</c>.
/// </summary>
public record HelloMessageResponse(string Message);
