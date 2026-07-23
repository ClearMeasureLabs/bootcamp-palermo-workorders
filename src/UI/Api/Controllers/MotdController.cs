using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes the configured message of the day for operators, integrations, and automated probes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/motd")]
[Route($"{ApiRoutes.VersionedApiPrefix}/motd")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MotdController(IOptions<MotdOptions> motdOptions) : ControllerBase
{
    /// <summary>
    /// Returns the configured message of the day as JSON. Whitespace-only configuration yields <c>"message": ""</c>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = new MotdResponse(Message: motdOptions.Value.Message);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/motd</c> and <c>GET /api/v1.0/motd</c>.
/// </summary>
public record MotdResponse(string Message);
