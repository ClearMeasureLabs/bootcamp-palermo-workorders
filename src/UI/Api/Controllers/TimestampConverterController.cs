using System.Globalization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private const long MillisecondsEpochThreshold = 100_000_000_000L;

    /// <summary>
    /// Parses <paramref name="value"/> as a Unix epoch (seconds or milliseconds) or ISO-8601 instant and returns both representations.
    /// </summary>
    /// <param name="value">Required query value: integer string for Unix time (ms if absolute value ≥ 1e11, else seconds) or an ISO-8601 instant.</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TimestampConverterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Problem(
                detail: "Query parameter 'value' is required and cannot be empty or whitespace.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseInstant(trimmed, out var instant, out var errorDetail))
        {
            return Problem(detail: errorDetail, statusCode: StatusCodes.Status400BadRequest);
        }

        var utc = instant.ToOffset(TimeSpan.Zero);
        var payload = TimestampConverterResponse.FromUtc(utc);
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }

    internal static bool TryParseInstant(string trimmed, out DateTimeOffset instant, out string errorDetail)
    {
        instant = default;
        errorDetail = string.Empty;

        if (IsIntegerOnlyString(trimmed))
        {
            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch))
            {
                errorDetail = "The value is not a valid Unix epoch integer.";
                return false;
            }

            var magnitude = epoch >= 0 ? epoch : checked(-epoch);
            try
            {
                instant = magnitude >= MillisecondsEpochThreshold
                    ? DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                    : DateTimeOffset.FromUnixTimeSeconds(epoch);
            }
            catch (ArgumentOutOfRangeException)
            {
                errorDetail = "The Unix epoch value is out of the supported range.";
                return false;
            }

            return true;
        }

        if (DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out instant))
        {
            return true;
        }

        errorDetail = "The value is not a valid ISO-8601 instant.";
        return false;
    }

    private static bool IsIntegerOnlyString(string s)
    {
        if (s.Length == 0)
            return false;

        var i = 0;
        if (s[0] == '-')
        {
            i = 1;
            if (i == s.Length)
                return false;
        }

        for (; i < s.Length; i++)
        {
            var c = s[i];
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}
