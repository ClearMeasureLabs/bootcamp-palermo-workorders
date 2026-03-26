using ClearMeasure.Bootcamp.UI.Server.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class DetailedHealthController : ControllerBase
{
    /// <summary>
    /// Returns component-level health (mock entries until probes are implemented).
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthReportResponse), StatusCodes.Status200OK)]
    public ActionResult<DetailedHealthReportResponse> GetDetailed() =>
        Ok(HealthReportBuilder.Build());
}
