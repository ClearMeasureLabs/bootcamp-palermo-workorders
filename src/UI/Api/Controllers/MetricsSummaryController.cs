using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON snapshot of process runtime metrics for operators and monitoring tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics/summary")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics/summary")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(
    TimeProvider timeProvider,
    IRequestMetricsCounter requestMetricsCounter) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total requests served, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var payload = MetricsSummaryBuilder.Build(timeProvider, requestMetricsCounter);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
