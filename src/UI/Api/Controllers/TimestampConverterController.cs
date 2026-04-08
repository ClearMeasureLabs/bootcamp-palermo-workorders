using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch and ISO-8601 timestamps for operators and integrations.
/// Public under <c>/api/*</c> like <see cref="TimeController"/> and <see cref="VersionController"/>:
/// optional API key validation is skipped for this route when enabled, and the sliding-window rate limiter applies.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class TimestampConverterController : ControllerBase
{
    private const long MillisecondsThreshold = 1_000_000_000_000L;

    /// <summary>
    /// Converts a Unix epoch (seconds or milliseconds) or ISO-8601 string to all supported representations.
    /// </summary>
    /// <param name="timestamp">Required. Unix epoch as integer (seconds or milliseconds) or ISO-8601 instant string.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return Problem(
                detail: "Query parameter 'timestamp' is required. Supply a Unix epoch (seconds or milliseconds) or an ISO-8601 string.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var trimmed = timestamp.Trim();
        if (TryParseEpoch(trimmed, out var fromEpoch, out var epochUnit))
        {
            return OkPayload(fromEpoch, epochUnit);
        }

        if (DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return OkPayload(parsed, "iso8601");
        }

        return Problem(
            detail: $"Could not parse '{trimmed}' as a Unix epoch (seconds or milliseconds) or ISO-8601.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static bool TryParseEpoch(string s, out DateTimeOffset instant, out string inputKind)
    {
        instant = default;
        inputKind = "";

        if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return false;
        }

        if (n >= MillisecondsThreshold || n <= -MillisecondsThreshold)
        {
            instant = DateTimeOffset.FromUnixTimeMilliseconds(n);
            inputKind = "epoch_milliseconds";
            return true;
        }

        instant = DateTimeOffset.FromUnixTimeSeconds(n);
        inputKind = "epoch_seconds";
        return true;
    }

    private IActionResult OkPayload(DateTimeOffset instantUtc, string inputKind)
    {
        var utc = instantUtc.ToUniversalTime();
        var payload = new TimestampConverterResponse(
            InputKind: inputKind,
            UnixSeconds: utc.ToUnixTimeSeconds(),
            UnixMilliseconds: utc.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            UtcSortable: utc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " UTC",
            Rfc1123: utc.UtcDateTime.ToString("R", CultureInfo.InvariantCulture));

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
public record TimestampConverterResponse(
    string InputKind,
    long UnixSeconds,
    long UnixMilliseconds,
    string Iso8601Utc,
    string UtcSortable,
    string Rfc1123);
