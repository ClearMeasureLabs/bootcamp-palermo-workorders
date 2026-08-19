using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch and ISO-8601 timestamps for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Converts a timestamp supplied as <paramref name="epoch"/> (Unix seconds or milliseconds) or <paramref name="iso"/> (ISO-8601).
    /// Epoch values with absolute magnitude ≥ 1_000_000_000_000 are interpreted as milliseconds; otherwise seconds.
    /// </summary>
    /// <param name="epoch">Optional Unix epoch (seconds or milliseconds).</param>
    /// <param name="iso">Optional ISO-8601 string.</param>
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
