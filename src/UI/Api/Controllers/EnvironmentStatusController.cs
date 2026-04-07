using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a redacted snapshot of runtime and selected environment variables for operators.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController(IHostEnvironment hostEnvironment, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns OS, CLR, processor count, and selected environment variable names with redacted values.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = OutputCachePolicyNames.VersionMetadata)]
    public IActionResult Get()
    {
        var payload = EnvironmentStatusBuilder.Build(hostEnvironment, configuration);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
