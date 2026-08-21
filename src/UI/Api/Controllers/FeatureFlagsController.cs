using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns a snapshot of application feature flags and their configured enabled/disabled state.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class FeatureFlagsController(IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions)
    : ControllerBase
{
    /// <summary>
    /// Lists all cataloged feature flags and their current boolean values from configuration.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var snapshot = FeatureFlagsCatalog.BuildSnapshot(featureFlagsOptions.Value);
        var payload = new FeatureFlagsResponse(snapshot);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
