using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Stateless conversion between Unix epoch timestamps and ISO-8601 instants for operators and scripts.
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
    /// Converts either <paramref name="iso"/> (ISO-8601 / XML round-trip) or <paramref name="unix"/> (numeric string).
    /// </summary>
    /// <remarks>
    /// Exactly one query parameter must be supplied. Numeric <c>unix</c> uses seconds when
    /// <c>Abs(value) &lt; 1_000_000_000_000</c>, otherwise milliseconds. Parsing uses invariant culture after trim.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? iso, [FromQuery] string? unix)
    {
        var isoNormalized = NormalizeQuery(iso);
        var unixNormalized = NormalizeQuery(unix);
        var hasIso = isoNormalized.Length > 0;
        var hasUnix = unixNormalized.Length > 0;

        if (!hasIso && !hasUnix)
        {
            return Problem(
                detail: "Specify exactly one query parameter: `iso` (ISO-8601) or `unix` (epoch seconds or milliseconds).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (hasIso && hasUnix)
        {
            return Problem(
                detail: "Specify only one of `iso` or `unix`, not both.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        DateTimeOffset instant;
        if (hasUnix)
        {
            if (!long.TryParse(unixNormalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochValue))
            {
                return Problem(
                    detail: "The `unix` value must be a whole integer.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                instant = Math.Abs(epochValue) >= MillisecondsInterpretationThreshold
                    ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
                    : DateTimeOffset.FromUnixTimeSeconds(epochValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Problem(
                    detail: "The `unix` value represents an instant outside the supported range.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
        else if (!DateTimeOffset.TryParse(
                     isoNormalized,
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.RoundtripKind,
                     out instant))
        {
            return Problem(
                detail: "The `iso` value could not be parsed as ISO-8601.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var utc = instant.UtcDateTime;
        var payload = new TimestampConverterResponse(
            UtcIso8601: utc.ToString("O", CultureInfo.InvariantCulture),
            UnixSeconds: instant.ToUnixTimeSeconds(),
            UnixMilliseconds: instant.ToUnixTimeMilliseconds(),
            UtcRfc1123: utc.ToString("R", CultureInfo.InvariantCulture),
            UtcInvariantFormatted: utc.ToString("F", CultureInfo.InvariantCulture));

        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return ConditionalGetEtag.JsonContent(payload);
    }

    private static string NormalizeQuery(string? value) => value?.Trim() ?? "";
}

/// <summary>
/// JSON payload for timestamp conversion endpoints: canonical UTC ISO-8601, Unix seconds/ms, and invariant human-readable fields.
/// </summary>
public sealed record TimestampConverterResponse(
    string UtcIso8601,
    long UnixSeconds,
    long UnixMilliseconds,
    string UtcRfc1123,
    string UtcInvariantFormatted);
