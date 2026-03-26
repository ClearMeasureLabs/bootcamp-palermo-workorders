using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[Route("api/health")]
public class DetailedHealthController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("detailed")]
    public ActionResult<DetailedHealthReport> GetDetailed()
    {
        return Ok(HealthReportBuilder.Build(timeProvider));
    }
}
