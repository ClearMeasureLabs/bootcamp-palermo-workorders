using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime application feature flag status for operations and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON map of feature flag names to enabled/disabled status.
    /// </summary>
    [HttpGet]
    public IActionResult Get() =>
        ConditionalGetEtag.JsonContent(ApplicationFeatureFlags.GetAll());
}
