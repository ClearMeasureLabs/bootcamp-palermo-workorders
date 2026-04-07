using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class MetricsSummaryController(
    TimeProvider timeProvider,
    ApiRequestMetricsState requestMetrics) : ControllerBase
{
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var payload = MetricsSummaryBuilder.Build(timeProvider, requestMetrics.TotalRequestsServed);
        return Ok(payload);
    }
}
