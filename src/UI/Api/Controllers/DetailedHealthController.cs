using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/health")]
[Route($"{ApiRoutes.VersionedApiPrefix}/health")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class DetailedHealthController(
    TimeProvider timeProvider,
    IDetailedHealthReportProvider detailedHealthReportProvider) : ControllerBase
{
    [HttpGet]
    public ActionResult<SimpleHealthResponse> Get()
    {
        return Ok(SimpleHealthResponseBuilder.Build(timeProvider));
    }

    [HttpGet("detailed")]
    public async Task<ActionResult<DetailedHealthReport>> GetDetailed(CancellationToken cancellationToken)
    {
        var report = await detailedHealthReportProvider.GetReportAsync(cancellationToken);
        return Ok(report);
    }
}
