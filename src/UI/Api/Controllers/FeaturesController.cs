using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature flag status for operations and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeaturesController : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON map of every feature flag name to its current enabled/disabled state.
    /// </summary>
    [HttpGet("flags")]
    public IActionResult Get()
    {
        var payload = FeatureFlagRegistry.GetSnapshot();
        return ConditionalGetEtag.JsonContent(payload);
    }
}
