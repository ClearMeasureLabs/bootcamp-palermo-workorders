using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch values and ISO-8601 timestamps for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Converts a single timestamp input to epoch seconds, epoch milliseconds, ISO-8601, and formatted UTC/local strings.
    /// </summary>
    /// <param name="epoch">
    /// Unix timestamp in seconds or milliseconds. Values with absolute magnitude at or above
    /// <see cref="TimestampConverter.MillisecondsThreshold"/> are interpreted as milliseconds; otherwise seconds.
    /// </param>
    /// <param name="iso">ISO-8601 timestamp string (round-trip kind).</param>
    /// <returns>Both representations plus human-readable UTC and local formatted strings.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] long? epoch, [FromQuery] string? iso)
    {
        var parseResult = TimestampConverter.TryParse(epoch, iso);
        if (!parseResult.Success)
        {
            return Problem(detail: parseResult.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        var payload = TimestampConverter.BuildResponse(parseResult.Instant);
        return ConditionalGetEtag.JsonContent(payload);
    }
}
