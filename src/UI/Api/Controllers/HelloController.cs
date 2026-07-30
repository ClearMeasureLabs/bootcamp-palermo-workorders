using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Provides a simple greeting endpoint for API testing and health verification.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/hello")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
[AllowAnonymous]
public class HelloController : ControllerBase
{
    /// <summary>
    /// Returns a simple greeting message.
    /// </summary>
    /// <returns>A JSON object containing a greeting message.</returns>
    /// <response code="200">Returns the greeting message</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello, World!" });
    }
}
