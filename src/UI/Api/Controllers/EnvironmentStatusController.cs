using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON snapshot of the process runtime environment for operations and support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController : ControllerBase
{
    /// <summary>
    /// Returns OS description, processor count, CLR/framework version, and redacted allowlisted environment variable names.
    /// </summary>
    [HttpGet("environment")]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusSnapshot.Build();
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
