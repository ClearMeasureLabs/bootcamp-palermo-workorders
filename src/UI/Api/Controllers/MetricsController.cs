using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a JSON summary of process runtime metrics for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsController(
    TimeProvider timeProvider,
    IHttpRequestMetricsCounter requestCounter) : ControllerBase
{
    /// <summary>
    /// Returns uptime, total requests served, memory usage, and GC collection counts.
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var payload = MetricsSummaryBuilder.Build(timeProvider, requestCounter);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
