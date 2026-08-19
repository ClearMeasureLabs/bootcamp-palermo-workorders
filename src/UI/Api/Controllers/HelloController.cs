using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal JSON probe for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HelloController : ControllerBase
{
    /// <summary>
    /// Returns a greeting JSON payload for reachability checks.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = new HelloResponse("Hello, World!");
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/hello</c> and <c>GET /api/v1.0/hello</c>.
/// </summary>
public record HelloResponse(string Message);
