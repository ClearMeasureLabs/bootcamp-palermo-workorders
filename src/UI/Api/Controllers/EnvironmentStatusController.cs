using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes host and runtime environment diagnostics for operators and support tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController(
    IHostEnvironment hostEnvironment,
    IOptions<EnvironmentStatusOptions> options) : ControllerBase
{
    /// <summary>
    /// Returns OS, CLR, processor count, host environment name, and redacted curated environment variables.
    /// </summary>
    [HttpGet("environment")]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusBuilder.Build(hostEnvironment, options.Value);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return ConditionalGetEtag.JsonContent(payload);
    }
}
