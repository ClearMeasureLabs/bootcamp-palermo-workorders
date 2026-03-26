using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[Route("api/health")]
public class DetailedHealthController(HealthCheckService healthCheckService, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("detailed")]
    public async Task<ActionResult<DetailedHealthReport>> GetDetailed(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        return Ok(HealthReportBuilder.FromHealthReport(report, timeProvider));
    }
}
