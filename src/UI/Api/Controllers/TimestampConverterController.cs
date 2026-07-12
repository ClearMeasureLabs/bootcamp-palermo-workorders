using System.Net.Mime;
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
    /// Converts a Unix epoch value or ISO-8601 string to normalized machine-readable and human-readable formats.
    /// Local time reflects the server host time zone, not the caller's time zone.
    /// </summary>
    /// <param name="epoch">Unix epoch in seconds or milliseconds (auto-detected by magnitude).</param>
    /// <param name="iso">ISO-8601 date-time string (for example <c>2026-07-12T15:00:00Z</c>).</param>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? epoch, [FromQuery] string? iso)
    {
        var hasEpoch = !string.IsNullOrWhiteSpace(epoch);
        var hasIso = !string.IsNullOrWhiteSpace(iso);

        if (!hasEpoch && !hasIso)
        {
            return Problem(
                detail: "Provide exactly one query parameter: epoch (Unix seconds or milliseconds) or iso (ISO-8601 string).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "The epoch and iso query parameters are mutually exclusive; provide only one.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = hasEpoch
            ? TimestampConverter.TryConvertFromEpoch(epoch)
            : TimestampConverter.TryConvertFromIso(iso);

        if (!result.Success)
        {
            return Problem(detail: result.ErrorDetail, statusCode: StatusCodes.Status400BadRequest);
        }

        return ConditionalGetEtag.JsonContent(result.Payload!);
    }
}
