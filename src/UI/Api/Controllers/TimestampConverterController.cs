using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch timestamps and ISO-8601 strings for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Converts a Unix epoch or ISO-8601 timestamp into both representations plus human-readable UTC and local strings.
    /// </summary>
    /// <param name="epoch">
    /// Unix epoch as seconds or milliseconds. Values with absolute magnitude ≥ 1,000,000,000,000 are treated as milliseconds.
    /// </param>
    /// <param name="iso">ISO-8601 timestamp string.</param>
    /// <returns>Both epoch and ISO forms plus formatted UTC/local strings.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var parseResult = TimestampConverter.TryParse(epoch, iso);
        if (!parseResult.Success)
        {
            return Problem(detail: parseResult.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        var payload = TimestampConverter.ToResponse(parseResult.Instant);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
