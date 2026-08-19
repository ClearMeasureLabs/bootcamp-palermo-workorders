using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes runtime environment diagnostics (OS, processor count, CLR, redacted env vars) for operators and support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class StatusEnvironmentController(IOptions<EnvironmentDiagnosticsOptions> options) : ControllerBase
{
    /// <summary>
    /// Returns a JSON snapshot of the runtime environment with allowlisted environment variable names (values redacted).
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusBuilder.Build(options);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return ConditionalGetEtag.JsonContent(payload);
    }
}
