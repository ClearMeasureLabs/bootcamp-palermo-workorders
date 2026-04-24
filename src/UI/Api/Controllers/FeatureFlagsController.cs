using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes resolved configuration feature flags for runtime clients and operators.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController(IOptions<DiagnosticsFeatureFlagsOptions> featureFlagsOptions) : ControllerBase
{
    /// <summary>
    /// Returns the current <see cref="DiagnosticsFeatureFlagsOptions"/> bound from configuration.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = new RuntimeFeatureFlagsResponse(featureFlagsOptions.Value);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
