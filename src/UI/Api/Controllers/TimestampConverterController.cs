using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch values and ISO-8601 UTC instants for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class TimestampConverterController : ControllerBase
{
    /// <summary>
    /// Converts exactly one supplied value: query <paramref name="epoch"/> (unix seconds or milliseconds via heuristic), or query <paramref name="iso"/>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] long? epoch, [FromQuery] string? iso)
    {
        var hasEpoch = epoch.HasValue;
        var isoTrimmed = iso?.Trim() ?? string.Empty;
        var hasIso = isoTrimmed.Length > 0;

        if (!hasEpoch && !hasIso)
        {
            return Problem(
                detail: "Provide exactly one query parameter: 'epoch' (unix seconds or milliseconds) or 'iso' (ISO-8601 instant).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "Provide only one of 'epoch' or 'iso', not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        DateTimeOffset instant;
        try
        {
            if (hasEpoch)
            {
                instant = TimestampConverterParsing.ParseUnixEpochNumeric(epoch!.Value).ToUniversalTime();
            }
            else
            {
                if (!TimestampConverterParsing.TryParseIso8601(isoTrimmed, out instant))
                {
                    return Problem(
                        detail: "The 'iso' value could not be parsed as an ISO-8601 instant.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                instant = instant.ToUniversalTime();
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(
                detail: "The 'epoch' value is outside the representable UTC range.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var seconds = instant.ToUnixTimeSeconds();
        var ms = instant.ToUnixTimeMilliseconds();
        var iso8601Utc = instant.ToString("O", CultureInfo.InvariantCulture);
        var rfc1123Utc = instant.ToString("R", CultureInfo.InvariantCulture);

        var payload = new TimestampConverterResponse(
            UnixEpochSeconds: seconds,
            UnixEpochMilliseconds: ms,
            Iso8601Utc: iso8601Utc,
            Rfc1123Utc: rfc1123Utc,
            UtcDisplay: instant.UtcDateTime.ToString("dddd, dd MMMM yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

        return ConditionalGetEtag.JsonContent(payload);
    }
}
