using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/health")]
[Route($"{ApiRoutes.VersionedApiPrefix}/health")]
public class DetailedHealthController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public ActionResult<SimpleHealthResponse> Get()
    {
        return Ok(SimpleHealthResponseBuilder.Build(timeProvider));
    }

    [HttpGet("detailed")]
    public ActionResult<DetailedHealthReport> GetDetailed()
    {
        return Ok(HealthReportBuilder.Build(timeProvider));
    }
}
