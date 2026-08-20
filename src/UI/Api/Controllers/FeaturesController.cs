using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes compile-time application feature flags for operations, CI, and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeaturesController : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON object of all feature flag names and their compile-time enabled/disabled status.
    /// </summary>
    [HttpGet]
    public IActionResult Get() =>
        ConditionalGetEtag.JsonContent(ApplicationFeatureFlags.All);
}
