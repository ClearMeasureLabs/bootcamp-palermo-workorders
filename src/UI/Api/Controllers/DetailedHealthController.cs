using Asp.Versioning;
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
    /// <summary>
    /// Lightweight liveness/readiness probe used by the load balancer and container orchestrator.
    /// Returns a simple timestamp JSON payload. Performs no deep dependency checks.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = SimpleHealthResponseBuilder.Build(timeProvider);
        return ConditionalJson(payload);
    }

    /// <summary>
    /// Runs all registered <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck"/> implementations.
    /// Intended for monitoring dashboards; not suitable for the load-balancer probe path.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed(CancellationToken cancellationToken)
    {
        var payload = await detailedHealthReportProvider.GetReportAsync(cancellationToken);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(DetailedHealthEtagFingerprint.FromReport(payload));
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }

    private IActionResult ConditionalJson<T>(T payload)
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload!);
    }
}
