using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch seconds and ISO-8601 UTC timestamps.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Converts the supplied <paramref name="epoch"/> or <paramref name="iso"/> query parameter
    /// into epoch, ISO-8601, and human-readable representations.
    /// </summary>
    /// <param name="epoch">Unix timestamp in seconds.</param>
    /// <param name="iso">ISO-8601 timestamp string.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "Supply either epoch or iso, not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!hasEpoch && !hasIso)
        {
            return Problem(
                detail: "Either epoch or iso query parameter is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch)
        {
            if (!TimestampConverter.TryParseEpoch(epoch!, out var dateTimeOffset, out var error))
            {
                return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
            }

            return OkWithEtag(TimestampConverter.ToResponse(dateTimeOffset));
        }

        if (!TimestampConverter.TryParseIso(iso!, out dateTimeOffset, out error))
        {
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        return OkWithEtag(TimestampConverter.ToResponse(dateTimeOffset));
    }

    private IActionResult OkWithEtag(TimestampConverterResponse payload)
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return ConditionalGetEtag.JsonContent(payload);
    }
}
