using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime feature flag states for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/features/flags")]
[Route($"{ApiRoutes.VersionedApiPrefix}/features/flags")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class FeatureFlagsController : ControllerBase
{
    /// <summary>
    /// Returns all application feature flags and whether each is enabled.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var snapshot = FeatureFlagsRegistry.GetSnapshot();
        var items = snapshot
            .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
            .Select(static kv => new FeatureFlagItem(kv.Key, kv.Value))
            .ToList();
        var payload = new FeatureFlagsResponse(items);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/features/flags</c> and <c>GET /api/v1.0/features/flags</c>.
/// </summary>
public record FeatureFlagsResponse(IReadOnlyList<FeatureFlagItem> Flags);

/// <summary>
/// One feature flag entry in <see cref="FeatureFlagsResponse"/>.
/// </summary>
public record FeatureFlagItem(string Key, bool Enabled);
