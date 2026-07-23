using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a minimal liveness probe for automated callers and load balancers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/healthz")]
[Route($"{ApiRoutes.VersionedApiPrefix}/healthz")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HealthzController : ControllerBase
{
    /// <summary>
    /// Returns HTTP 200 OK with an empty body when the API host is accepting requests.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok();
}
