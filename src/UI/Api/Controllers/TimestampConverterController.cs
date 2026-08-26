using System.Globalization;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Converts Unix seconds and ISO-8601 strings for operators and integrations.
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
            return "Provide exactly one of query parameters 'unix' or 'iso'.";
        }

        if (hasUnix && hasIso)
        {
            return "Provide exactly one of query parameters 'unix' or 'iso', not both.";
        }

        return null;
    }

    private static ParseInstantResult ParseInstant(bool hasUnix, string? unix, string? iso)
    {
        if (hasUnix)
        {
            return TryParseUnixSeconds(unix, out var instant)
                ? ParseInstantResult.FromInstant(instant)
                : ParseInstantResult.FromError(
                    "Query parameter 'unix' must be a 64-bit integer Unix timestamp in seconds.");
        }

        return TryParseIso(iso, out var parsedInstant)
            ? ParseInstantResult.FromInstant(parsedInstant)
            : ParseInstantResult.FromError(
                "Query parameter 'iso' must be a valid ISO-8601 date/time string.");
    }

    private static bool TryParseUnixSeconds(string? unix, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(unix))
        {
            return false;
        }

        if (!long.TryParse(unix.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        try
        {
            instant = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryParseIso(string? iso, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(iso))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                iso.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out instant))
        {
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
public record TimestampConverterResponse(long Unix, string Iso, string Human);
