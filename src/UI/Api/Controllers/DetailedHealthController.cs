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
    public IActionResult Get()
    {
        var payload = SimpleHealthResponseBuilder.Build(timeProvider);
        return ConditionalJson(payload);
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed(CancellationToken cancellationToken)
    {
        var payload = await detailedHealthReportProvider.GetReportAsync(cancellationToken);
        return ConditionalJson(payload);
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
