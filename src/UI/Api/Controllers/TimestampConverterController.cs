using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts between Unix epoch seconds and ISO-8601 strings for operators and integrations.
/// Accepts exactly one of <c>?unix=&lt;seconds&gt;</c> or <c>?iso=&lt;ISO-8601&gt;</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/timestamp-converter")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/timestamp-converter")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class TimestampConverterController : ControllerBase
{
    private const string IsoOutputFormat = "yyyy-MM-ddTHH:mm:ssZ";
    private const string HumanOutputFormat = "dddd, dd MMMM yyyy HH:mm:ss UTC";

    /// <summary>
    /// Absolute Unix-second magnitudes at or above this threshold are rejected as millisecond-scale
    /// (13+ digit epoch values). Valid second-range max is ~2.53e11; 1e12 is safely above that.
    /// </summary>
    private const long MillisecondScaleThresholdSeconds = 1_000_000_000_000L;

    private const string MissingParamDetail =
        "Provide exactly one of query parameters 'unix' (epoch seconds) or 'iso' (ISO-8601).";

    private const string BothParamsDetail =
        "Provide exactly one of query parameters 'unix' (epoch seconds) or 'iso' (ISO-8601), not both.";

    private const string UnixInvalidDetail =
        "Query parameter 'unix' must be a 64-bit integer Unix timestamp in seconds (not milliseconds).";

    private const string UnixMillisecondsDetail =
        "Query parameter 'unix' must be Unix epoch seconds, not milliseconds. Received a millisecond-scale value.";

    private const string UnixEmptyDetail =
        "Query parameter 'unix' is required when present; provide epoch seconds as a 64-bit integer.";

    private const string IsoInvalidDetail =
        "Query parameter 'iso' must be a valid ISO-8601 date/time string.";

    private const string IsoEmptyDetail =
        "Query parameter 'iso' is required when present; provide a valid ISO-8601 date/time string.";

    /// <summary>
    /// Returns Unix seconds, ISO-8601 UTC (second precision), and a human UTC display string
    /// for exactly one of <paramref name="unix"/> or <paramref name="iso"/>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? unix, [FromQuery] string? iso)
    {
        var queryError = ValidateQueryKeys(Request.Query);
        if (queryError != null)
        {
            return Problem(detail: queryError, statusCode: StatusCodes.Status400BadRequest);
        }

        var parseResult = ParseInstant(Request.Query.ContainsKey("unix"), unix, iso);
        if (!parseResult.Success)
        {
            return Problem(detail: parseResult.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(BuildResponse(parseResult.Instant));
    }

    private static string? ValidateQueryKeys(IQueryCollection query)
    {
        var hasUnix = query.ContainsKey("unix");
        var hasIso = query.ContainsKey("iso");

        if (!hasUnix && !hasIso)
        {
            return MissingParamDetail;
        }

        if (hasUnix && hasIso)
        {
            return BothParamsDetail;
        }

        return null;
    }

    private static ParseInstantResult ParseInstant(bool hasUnix, string? unix, string? iso)
    {
        if (hasUnix)
        {
            return TryParseUnixSeconds(unix, out var instant, out var error)
                ? ParseInstantResult.FromInstant(instant)
                : ParseInstantResult.FromError(error);
        }

        return TryParseIso(iso, out var parsedInstant, out var isoError)
            ? ParseInstantResult.FromInstant(parsedInstant)
            : ParseInstantResult.FromError(isoError);
    }

    private static bool TryParseUnixSeconds(string? unix, out DateTimeOffset instant, out string error)
    {
        instant = default;
        error = UnixInvalidDetail;

        if (string.IsNullOrWhiteSpace(unix))
        {
            error = UnixEmptyDetail;
            return false;
        }

        if (!long.TryParse(unix.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            error = UnixInvalidDetail;
            return false;
        }

        if (Math.Abs(seconds) >= MillisecondScaleThresholdSeconds)
        {
            error = UnixMillisecondsDetail;
            return false;
        }

        try
        {
            instant = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            error = UnixInvalidDetail;
            return false;
        }
    }

    private static bool TryParseIso(string? iso, out DateTimeOffset instant, out string error)
    {
        instant = default;
        error = IsoInvalidDetail;

        if (string.IsNullOrWhiteSpace(iso))
        {
            error = IsoEmptyDetail;
            return false;
        }

        if (!DateTimeOffset.TryParse(
                iso.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out instant))
        {
            error = IsoInvalidDetail;
            return false;
        }

        instant = instant.ToUniversalTime();
        return true;
    }

    private static TimestampConverterResponse BuildResponse(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new TimestampConverterResponse(
            Unix: utc.ToUnixTimeSeconds(),
            Iso: utc.ToString(IsoOutputFormat, CultureInfo.InvariantCulture),
            Human: utc.ToString(HumanOutputFormat, CultureInfo.InvariantCulture));
    }

    private readonly struct ParseInstantResult
    {
        private ParseInstantResult(bool success, DateTimeOffset instant, string? error)
        {
            Success = success;
            Instant = instant;
            Error = error;
        }

        public bool Success { get; }
        public DateTimeOffset Instant { get; }
        public string? Error { get; }

        public static ParseInstantResult FromInstant(DateTimeOffset instant) =>
            new(true, instant, null);

        public static ParseInstantResult FromError(string error) =>
            new(false, default, error);
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
/// <param name="Unix">Unix epoch seconds (UTC).</param>
/// <param name="Iso">ISO-8601 UTC string, second precision (e.g. <c>2023-11-14T22:13:20Z</c>).</param>
/// <param name="Human">Human-readable UTC display (e.g. <c>Tuesday, 14 November 2023 22:13:20 UTC</c>).</param>
public record TimestampConverterResponse(long Unix, string Iso, string Human);
