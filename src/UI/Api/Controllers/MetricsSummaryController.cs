using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes per-process runtime metrics (uptime, request totals, memory, GC) for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/metrics")]
[Route($"{ApiRoutes.VersionedApiPrefix}/metrics")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class MetricsSummaryController(
    TimeProvider timeProvider,
    IApplicationRuntimeMetricsSnapshot metricsSnapshot) : ControllerBase
{
    /// <summary>
    /// Returns uptime (same semantics as <see cref="SimpleHealthResponseBuilder"/>), total requests served,
    /// working set and GC heap snapshot, and GC collection counts for generations 0–2.
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var payload = metricsSnapshot.Build(timeProvider);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return ConditionalGetEtag.JsonContent(payload);
    }
}
