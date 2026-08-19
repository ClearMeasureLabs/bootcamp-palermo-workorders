using System.Globalization;
using System.Text;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates random values in plain text for scripts, tests, and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsRandomController : ControllerBase
{
    internal const int DefaultNumberMin = 0;
    internal const int DefaultNumberMax = 1000;
    internal const int DefaultStringLength = 10;
    internal const int MaxStringLength = 100;

    private const string NumberType = "number";
    private const string StringType = "string";
    private const string UuidType = "uuid";
    private const string ColorType = "color";

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        NumberType,
        StringType,
        UuidType,
        ColorType
    };

    private static readonly char[] StringAlphabet =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    /// <summary>
    /// Returns a random value for the requested <paramref name="type"/>.
    /// </summary>
    /// <param name="type">One of <c>number</c>, <c>string</c>, <c>uuid</c>, or <c>color</c>.</param>
    /// <param name="min">Inclusive lower bound for <c>number</c> (default 0).</param>
    /// <param name="max">Inclusive upper bound for <c>number</c> (default 1000).</param>
    /// <param name="length">Length for <c>string</c> (default 10, max 100).</param>
    [HttpGet]
    [AllowAnonymous]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get(
        [FromQuery] string? type,
        [FromQuery] int? min,
        [FromQuery] int? max,
        [FromQuery] int? length)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Valid values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!ValidTypes.Contains(type))
        {
            return Problem(
                detail: $"Invalid type '{type}'. Valid values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var normalizedType = type.ToLowerInvariant();
        string content;

        switch (normalizedType)
        {
            case NumberType:
            {
                var lower = min ?? DefaultNumberMin;
                var upper = max ?? DefaultNumberMax;
                if (lower > upper)
                {
                    return Problem(
                        detail: "'min' must be less than or equal to 'max'.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                content = Random.Shared.Next(lower, upper + 1).ToString(CultureInfo.InvariantCulture);
                break;
            }
            case StringType:
            {
                var stringLength = length ?? DefaultStringLength;
                if (stringLength > MaxStringLength)
                {
                    return Problem(
                        detail: $"'length' must not exceed {MaxStringLength}.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                if (stringLength <= 0)
                {
                    return Problem(
                        detail: "'length' must be greater than zero.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                content = GenerateRandomString(stringLength);
                break;
            }
            case UuidType:
                content = Guid.NewGuid().ToString("D");
                break;
            case ColorType:
                content = GenerateRandomColor();
                break;
            default:
                return Problem(
                    detail: $"Invalid type '{type}'. Valid values: number, string, uuid, color.",
                    statusCode: StatusCodes.Status400BadRequest);
        }

        return new ContentResult
        {
            Content = content,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }

    private static string GenerateRandomString(int stringLength)
    {
        var builder = new StringBuilder(stringLength);
        for (var i = 0; i < stringLength; i++)
        {
            builder.Append(StringAlphabet[Random.Shared.Next(StringAlphabet.Length)]);
        }

        return builder.ToString();
    }

    private static string GenerateRandomColor()
    {
        var r = Random.Shared.Next(0, 256);
        var g = Random.Shared.Next(0, 256);
        var b = Random.Shared.Next(0, 256);
        return FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
    }
}
