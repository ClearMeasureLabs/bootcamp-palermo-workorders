using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature-flag status from the static in-memory catalog for ops and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController : ControllerBase
{
    /// <summary>
    /// Returns a JSON object of all feature flags and their enabled/disabled status.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        ConditionalGetEtag.JsonContent(FeatureFlagsCatalog.GetAll());
}
