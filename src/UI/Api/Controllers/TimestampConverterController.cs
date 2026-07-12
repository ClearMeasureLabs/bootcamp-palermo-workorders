using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch and ISO-8601 timestamp representations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Accepts either <paramref name="epoch"/> or <paramref name="iso"/> and returns both machine formats plus human-readable displays.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (!hasEpoch && !hasIso)
        {
            return Problem(
                detail: "Supply exactly one query parameter: epoch or iso.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "Supply only one of epoch or iso, not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch)
        {
            if (!TimestampConverter.TryFromEpoch(epoch, out var instant, out var error))
            {
                return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
            }

            return ConditionalGetEtag.JsonContent(TimestampConverter.ToResponse(instant, "epoch"));
        }

        if (!TimestampConverter.TryFromIso8601(iso, out var isoInstant, out var isoError))
        {
            return Problem(detail: isoError, statusCode: StatusCodes.Status400BadRequest);
        }

        return ConditionalGetEtag.JsonContent(TimestampConverter.ToResponse(isoInstant, "iso"));
    }
}
