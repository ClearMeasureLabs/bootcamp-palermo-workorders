using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature flag status for operations and support tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController : ControllerBase
{
    /// <summary>
    /// Returns a JSON object of all known feature flag names mapped to enabled/disabled status.
    /// </summary>
    [HttpGet]
    public IActionResult Get() =>
        ConditionalGetEtag.JsonContent(FeatureFlagsCatalog.All);
}
