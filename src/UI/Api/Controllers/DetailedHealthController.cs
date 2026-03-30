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
    public IActionResult Get()
    {
        var payload = SimpleHealthResponseBuilder.Build(timeProvider);
        return ConditionalJson(payload);
    }

    [HttpGet("detailed")]
    public IActionResult GetDetailed()
    {
        var payload = HealthReportBuilder.Build(timeProvider);
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
