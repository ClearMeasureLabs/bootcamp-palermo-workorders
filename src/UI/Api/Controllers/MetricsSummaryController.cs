using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes live application runtime metrics for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(TimeProvider timeProvider, IRequestMetrics requestMetrics) : ControllerBase
{
    /// <summary>
    /// Returns uptime, request counts, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    public IActionResult GetSummary()
    {
        var payload = ApplicationRuntimeMetricsBuilder.Build(timeProvider, requestMetrics);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
