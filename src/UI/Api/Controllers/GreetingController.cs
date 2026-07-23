using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a simple greeting message for testing and demonstration.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/greeting")]
[Route($"{ApiRoutes.VersionedApiPrefix}/greeting")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class GreetingController : ControllerBase
{
    /// <summary>
    /// Returns a JSON greeting message.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        Ok(new { message = "Hello from Church Bulletin" });
}
