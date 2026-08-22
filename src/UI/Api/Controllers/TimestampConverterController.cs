using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch timestamps and ISO-8601 UTC instants for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class TimestampConverterController : ControllerBase
{
    private const long MillisecondsInterpretationThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Accepts a single <paramref name="value"/> that is either a Unix epoch (seconds or milliseconds)
    /// or an ISO-8601 instant, and returns both formats plus human-readable UTC representations.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return Problem(
                detail: "Query parameter 'value' is required. Provide a Unix epoch (seconds or milliseconds) or an ISO-8601 instant.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseValue(normalized, out var instant, out var errorDetail))
        {
            return Problem(detail: errorDetail, statusCode: StatusCodes.Status400BadRequest);
        }

        var utc = instant.ToUniversalTime();
        var payload = new TimestampConverterResponse(
            UnixEpochSeconds: utc.ToUnixTimeSeconds(),
            UnixEpochMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.ToString("O", CultureInfo.InvariantCulture),
            Rfc1123Utc: utc.ToString("R", CultureInfo.InvariantCulture),
            UtcDisplay: utc.UtcDateTime.ToString("dddd, dd MMMM yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }

    private static bool TryParseValue(string normalized, out DateTimeOffset instant, out string errorDetail)
    {
        instant = default;
        errorDetail = string.Empty;

        if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochValue))
        {
            try
            {
                instant = Math.Abs(epochValue) >= MillisecondsInterpretationThreshold
                    ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
                    : DateTimeOffset.FromUnixTimeSeconds(epochValue);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                errorDetail = "The 'value' epoch is outside the representable UTC range.";
                return false;
            }
        }

        if (DateTimeOffset.TryParse(
                normalized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
                out instant))
        {
            return true;
        }

        errorDetail =
            "The 'value' parameter could not be parsed as a Unix epoch integer or an ISO-8601 instant.";
        return false;
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c> and versioned equivalent.
/// </summary>
public sealed record TimestampConverterResponse(
    long UnixEpochSeconds,
    long UnixEpochMilliseconds,
    string Iso8601Utc,
    string Rfc1123Utc,
    string UtcDisplay);
