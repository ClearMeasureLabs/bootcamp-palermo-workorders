using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch values and ISO-8601 timestamps for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController(
    ILogger<TimestampConverterController> logger,
    TimeZoneInfo? localTimeZone = null) : ControllerBase
{
    private readonly TimeZoneInfo _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;

    /// <summary>
    /// Converts a Unix epoch (<paramref name="epoch"/>) or ISO-8601 string (<paramref name="iso"/>) into both formats plus human-readable displays.
    /// </summary>
    /// <param name="epoch">Unix epoch in seconds or milliseconds.</param>
    /// <param name="iso">ISO-8601 timestamp.</param>
    [HttpGet]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "Provide exactly one query parameter: epoch or iso.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!hasEpoch && !hasIso)
        {
            return Problem(
                detail: "A query parameter is required: epoch or iso.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        DateTimeOffset instant;
        try
        {
            instant = hasEpoch
                ? TimestampConverter.ParseEpoch(epoch!)
                : TimestampConverter.ParseIso(iso!);
        }
        catch (FormatException ex)
        {
            logger.LogDebug(ex, "Timestamp converter rejected invalid input.");
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        var payload = TimestampConverter.ToResponse(instant, _localTimeZone);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }
}
