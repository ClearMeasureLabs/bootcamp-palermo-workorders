using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route($"{ApiRoutes.VersionedApiPrefix}/hello")]
public class HelloController(ILogger<HelloController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        logger.LogDebug("Hello endpoint called");
        return Ok(new { message = "Hello, World!" });
    }
}
