using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route($"{ApiRoutes.VersionedApiPrefix}/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello, World!" });
    }
}
