using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hello")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHello()
    {
        return Ok(new { message = "Hello, World!" });
    }
}
