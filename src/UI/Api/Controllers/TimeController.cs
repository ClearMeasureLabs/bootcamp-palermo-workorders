using System.Globalization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/time")]
[Route($"{ApiRoutes.VersionedApiPrefix}/time")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimeController(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var text = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        var etag = ConditionalGetEtag.CreateWeakEtagForPlainText(text);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return new ContentResult
        {
            Content = text,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
