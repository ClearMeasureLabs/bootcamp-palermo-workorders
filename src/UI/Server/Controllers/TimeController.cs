using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class TimeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var utcNow = DateTime.UtcNow.ToString("O"); // ISO-8601 format
        return Ok(new { utc = utcNow });
    }
}
