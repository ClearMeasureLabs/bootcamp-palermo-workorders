using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EchoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] string? msg)
    {
        return Content(msg ?? string.Empty, "text/plain");
    }
}
