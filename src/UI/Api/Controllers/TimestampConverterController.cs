using System.Globalization;
using System.Text.RegularExpressions;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch timestamps and ISO-8601 instants for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController : ControllerBase
{
    private static readonly Regex EpochIntegerPattern = new(@"^-?\d+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Converts the supplied <paramref name="value"/> (Unix epoch seconds or milliseconds, or ISO-8601) to multiple representations.
    /// </summary>
    /// <param name="value">Unix epoch (integer seconds or milliseconds) or an ISO-8601 date/time string.</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BadRequest(new { error = "Query parameter 'value' is required." });

        var trimmed = value.Trim();
        if (!TryParseInstant(trimmed, out var instant, out var inputKind, out var error))
            return BadRequest(new { error = error });

        var utc = instant.UtcDateTime;
        var payload = new TimestampConverterResponse(
            InputKind: inputKind,
            UnixSeconds: instant.ToUnixTimeSeconds(),
            UnixMilliseconds: instant.ToUnixTimeMilliseconds(),
            Iso8601Utc: utc.ToString("O", CultureInfo.InvariantCulture),
            Iso8601WithOffset: instant.ToString("O", CultureInfo.InvariantCulture),
            UtcRfc1123: utc.ToString("R", CultureInfo.InvariantCulture),
            UtcLongDateString: utc.ToString("D", CultureInfo.InvariantCulture) + " " + utc.ToString("T", CultureInfo.InvariantCulture) + " UTC");

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }

    private static bool TryParseInstant(
        string trimmed,
        out DateTimeOffset instant,
        out string inputKind,
        out string? error)
    {
        instant = default;
        inputKind = "";
        error = null;

        if (EpochIntegerPattern.IsMatch(trimmed))
        {
            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                error = "Epoch value is not a valid integer.";
                return false;
            }

            var abs = n == long.MinValue ? ulong.MaxValue / 2 + 1 : (ulong)Math.Abs(n);
            var asMs = abs >= 10_000_000_000UL;
            try
            {
                instant = asMs
                    ? DateTimeOffset.FromUnixTimeMilliseconds(n)
                    : DateTimeOffset.FromUnixTimeSeconds(n);
            }
            catch (ArgumentOutOfRangeException)
            {
                error = "Epoch value is out of the supported date range.";
                return false;
            }

            inputKind = asMs ? "UnixEpochMilliseconds" : "UnixEpochSeconds";
            return true;
        }

        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            instant = parsed;
            inputKind = "Iso8601";
            return true;
        }

        error = "Value is not a recognized Unix epoch integer or ISO-8601 date/time.";
        return false;
    }
}

/// <summary>
/// JSON payload for timestamp conversion responses.
/// </summary>
public record TimestampConverterResponse(
    string InputKind,
    long UnixSeconds,
    long UnixMilliseconds,
    string Iso8601Utc,
    string Iso8601WithOffset,
    string UtcRfc1123,
    string UtcLongDateString);
