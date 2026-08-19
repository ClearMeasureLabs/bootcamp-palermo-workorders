using System.Globalization;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes configurable random data generation for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class RandomController(Random? random = null) : ControllerBase
{
    private const int DefaultNumberMin = 0;
    private const int DefaultNumberMax = 100;
    private const int DefaultStringLength = 16;
    private const int MaxStringLength = 1000;
    private const string AlphanumericCharset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// Returns random data of the requested <paramref name="type"/> (<c>number</c>, <c>string</c>, <c>uuid</c>, or <c>color</c>).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get(
        [FromQuery] string? type,
        [FromQuery] string? min,
        [FromQuery] string? max,
        [FromQuery] string? length,
        [FromQuery] string? format)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequestJson("type parameter required");
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "number" => GenerateNumber(min, max),
            "string" => GenerateString(length),
            "uuid" => OkJson(new RandomValueResponse("uuid", Guid.NewGuid().ToString())),
            "color" => GenerateColor(format),
            _ => BadRequestJson("type must be one of: number, string, uuid, color")
        };
    }

    private IActionResult GenerateNumber(string? minText, string? maxText)
    {
        var minValue = DefaultNumberMin;
        if (!string.IsNullOrWhiteSpace(minText))
        {
            if (!int.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out minValue))
            {
                return BadRequestJson("min must be an integer");
            }
        }

        var maxValue = DefaultNumberMax;
        if (!string.IsNullOrWhiteSpace(maxText))
        {
            if (!int.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxValue))
            {
                return BadRequestJson("max must be an integer");
            }
        }

        if (minValue > maxValue)
        {
            return BadRequestJson("min must be less than or equal to max");
        }

        var value = _random.Next(minValue, maxValue + 1);
        return OkJson(new RandomValueResponse("number", value));
    }

    private IActionResult GenerateString(string? lengthText)
    {
        var stringLength = DefaultStringLength;
        if (!string.IsNullOrWhiteSpace(lengthText))
        {
            if (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out stringLength))
            {
                return BadRequestJson("length must be an integer");
            }

            if (stringLength < 1 || stringLength > MaxStringLength)
            {
                return BadRequestJson($"length must be between 1 and {MaxStringLength}");
            }
        }

        var builder = new StringBuilder(stringLength);
        for (var i = 0; i < stringLength; i++)
        {
            var index = _random.Next(AlphanumericCharset.Length);
            builder.Append(AlphanumericCharset[index]);
        }

        return OkJson(new RandomValueResponse("string", builder.ToString()));
    }

    private IActionResult GenerateColor(string? format)
    {
        if (!string.IsNullOrWhiteSpace(format)
            && !format.Trim().Equals("hex", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequestJson("format must be hex when specified");
        }

        var red = _random.Next(0, 256);
        var green = _random.Next(0, 256);
        var blue = _random.Next(0, 256);
        var hex = $"#{red:X2}{green:X2}{blue:X2}";
        return OkJson(new RandomValueResponse("color", hex));
    }

    private ContentResult OkJson(RandomValueResponse payload) => ConditionalGetEtag.JsonContent(payload);

    private ContentResult BadRequestJson(string message) =>
        new()
        {
            Content = JsonSerializer.Serialize(new RandomErrorResponse(message), ConditionalGetEtag.JsonSerializerOptions),
            ContentType = "application/json; charset=utf-8",
            StatusCode = StatusCodes.Status400BadRequest
        };
}

/// <summary>
/// JSON payload for successful <c>GET /api/tools/random</c> responses.
/// </summary>
public record RandomValueResponse(string Type, object Value);

/// <summary>
/// JSON payload for validation errors from <c>GET /api/tools/random</c>.
/// </summary>
public record RandomErrorResponse(string Error);
