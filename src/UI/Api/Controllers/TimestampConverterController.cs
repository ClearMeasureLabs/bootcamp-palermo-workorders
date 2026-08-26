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
        var hasUnix = Request.Query.ContainsKey("unix");
        var hasIso = Request.Query.ContainsKey("iso");

        if (!hasUnix && !hasIso)
        {
            return Problem(
                detail: "Provide exactly one of query parameters 'unix' or 'iso'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasUnix && hasIso)
        {
            return Problem(
                detail: "Provide exactly one of query parameters 'unix' or 'iso', not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        DateTimeOffset instant;
        if (hasUnix)
        {
            if (!TryParseUnixSeconds(unix, out instant))
            {
                return Problem(
                    detail: "Query parameter 'unix' must be a 64-bit integer Unix timestamp in seconds.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
        else if (!TryParseIso(iso, out instant))
        {
            return Problem(
                detail: "Query parameter 'iso' must be a valid ISO-8601 date/time string.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(BuildResponse(instant));
    }

    internal static bool TryParseUnixSeconds(string? unix, out DateTimeOffset instant)
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

    internal static bool TryParseIso(string? iso, out DateTimeOffset instant)
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

    internal static TimestampConverterResponse BuildResponse(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new TimestampConverterResponse(
            Unix: utc.ToUnixTimeSeconds(),
            Iso: utc.ToString(IsoOutputFormat, CultureInfo.InvariantCulture),
            Human: utc.ToString(HumanOutputFormat, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/timestamp-converter</c>.
/// </summary>
public record TimestampConverterResponse(long Unix, string Iso, string Human);
