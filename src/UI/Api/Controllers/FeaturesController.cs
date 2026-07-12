using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature-flag status for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeaturesController(IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON object of all cataloged feature flags and their current enabled/disabled status.
    /// </summary>
    [HttpGet("flags")]
    public IActionResult GetFlags()
    {
        var payload = FeatureFlagStatusResolver.Resolve(featureFlagsOptions.Value);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
