using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature flag status for operations, scripts, and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeaturesController : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON map of flag names to enabled/disabled status.
    /// </summary>
    [HttpGet("flags")]
    [AllowAnonymous]
    public IActionResult GetFlags()
    {
        var payload = ApplicationFeatureFlags.Flags;
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
