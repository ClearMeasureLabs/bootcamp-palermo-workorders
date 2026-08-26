using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts Unix epoch timestamps and ISO-8601 strings for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsTimestampConverterController : ControllerBase
{
    /// <summary>
    /// Threshold above which an epoch value is treated as milliseconds rather than seconds.
    /// </summary>
    internal const long MillisecondsThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Returns epoch seconds, epoch milliseconds, ISO-8601 UTC, RFC 1123, and a UTC display string
    /// for exactly one of <paramref name="epoch"/> or <paramref name="iso"/>.
    /// </summary>
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
                detail: "Provide exactly one of query parameters 'epoch' or 'iso'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasEpoch && hasIso)
        {
            return Problem(
                detail: "Provide exactly one of query parameters 'epoch' or 'iso', not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        DateTimeOffset instant;
        if (hasEpoch)
        {
            if (!TryParseEpoch(epoch!, out instant))
            {
                return Problem(
                    detail: "Query parameter 'epoch' must be a Unix timestamp in seconds or milliseconds.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
        else if (!TryParseIso(iso!, out instant))
        {
            return Problem(
                detail: "Query parameter 'iso' must be a valid ISO-8601 date/time string.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(BuildResponse(instant));
    }

    internal static bool TryParseEpoch(string epoch, out DateTimeOffset instant)
    {
        instant = default;
        if (!long.TryParse(epoch.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        try
        {
            instant = value >= MillisecondsThreshold
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    internal static bool TryParseIso(string iso, out DateTimeOffset instant)
    {
        return DateTimeOffset.TryParse(
            iso.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out instant);
    }

    internal static TimestampConverterResponse BuildResponse(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new TimestampConverterResponse(
            EpochSeconds: utc.ToUnixTimeSeconds(),
            EpochMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601: utc.ToString("O", CultureInfo.InvariantCulture),
            Rfc1123: utc.ToString("R", CultureInfo.InvariantCulture),
            UnixUtcDisplay: utc.ToString("dddd, MMMM d, yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
public record TimestampConverterResponse(
    long EpochSeconds,
    long EpochMilliseconds,
    string Iso8601,
    string Rfc1123,
    string UnixUtcDisplay);
