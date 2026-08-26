using System.Globalization;
using System.Net.Mime;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates configurable random values for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsRandomController : ControllerBase
{
    private const string AlphanumericChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int AlphanumericLength = 12;

    private static readonly Dictionary<string, Func<string>> Generators =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["number"] = () => Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["string"] = GenerateAlphanumeric,
            ["uuid"] = static () => Guid.NewGuid().ToString("D"),
            ["color"] = static () => $"#{Random.Shared.Next(0x1000000):X6}"
        };

    /// <summary>
    /// Returns a random value as plain text for the requested <paramref name="type"/>.
    /// Supported types: <c>number</c>, <c>string</c>, <c>uuid</c>, <c>color</c>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Supported values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Generators.TryGetValue(type.Trim(), out var generator))
        {
            return Problem(
                detail: $"Unknown type '{type}'. Supported values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return new ContentResult
        {
            Content = generator(),
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }

    private static string GenerateAlphanumeric()
    {
        var buffer = new StringBuilder(AlphanumericLength);
        for (var i = 0; i < AlphanumericLength; i++)
        {
            buffer.Append(AlphanumericChars[Random.Shared.Next(AlphanumericChars.Length)]);
        }

        return buffer.ToString();
    }
}
