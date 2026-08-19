using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch seconds and ISO-8601 timestamps for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController(TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Converts the supplied <paramref name="epoch"/> or <paramref name="iso"/> query parameter to both formats plus human-readable fields.
    /// </summary>
    /// <param name="epoch">Unix epoch timestamp in seconds.</param>
    /// <param name="iso">ISO-8601 timestamp string.</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var outcome = TimestampConverter.Convert(epoch, iso, timeProvider);
        if (!outcome.Succeeded)
            return BadRequest(new { error = outcome.Error });

        var payload = outcome.Payload!;
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }
}
