using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature-flag status as a flat JSON object for operations and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController(IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : ControllerBase
{
    /// <summary>
    /// Returns a flat JSON object mapping feature-flag names to enabled/disabled booleans.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var snapshot = FeatureFlagsSnapshot.FromOptions(featureFlagsOptions.Value);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(snapshot);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(snapshot);
    }
}
