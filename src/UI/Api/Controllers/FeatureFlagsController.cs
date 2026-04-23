using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature-flag state for operators and automation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController : ControllerBase
{
    /// <summary>
    /// Returns all configured feature flags and whether each is enabled.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = ApplicationFeatureFlags.All
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
