using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates random scalar values for tests and tooling. Requires query <c>type</c>:
/// <c>number</c>, <c>string</c>, <c>uuid</c>, or <c>color</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsRandomController : ControllerBase
{
    /// <summary>
    /// Default length for generated <c>string</c> payloads (URL-safe alphanumeric).
    /// </summary>
    public const int DefaultStringLength = 24;

    private const string UrlSafeAlphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>
    /// Returns a JSON object <c>{{ "type": "...", "value": ... }}</c>.
    /// <list type="bullet">
    /// <item><description><c>number</c> — random signed 32-bit integer.</description></item>
    /// <item><description><c>string</c> — random URL-safe alphanumeric string of length <see cref="DefaultStringLength"/>.</description></item>
    /// <item><description><c>uuid</c> — <see cref="Guid"/> standard string representation.</description></item>
    /// <item><description><c>color</c> — six-digit hexadecimal color <c>#RRGGBB</c>.</description></item>
    /// </list>
    /// The <paramref name="type"/> parameter is matched case-insensitively.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: "Query parameter 'type' is required. Allowed values: number, string, uuid, color.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        switch (type.Trim().ToLowerInvariant())
        {
            case "number":
            {
                var value = unchecked((int)Random.Shared.NextInt64(int.MinValue, (long)int.MaxValue + 1));
                return ConditionalGetEtag.JsonContent(new { type = "number", value });
            }
            case "string":
                return ConditionalGetEtag.JsonContent(new { type = "string", value = GenerateUrlSafeAlphanumeric(DefaultStringLength) });
            case "uuid":
                return ConditionalGetEtag.JsonContent(new { type = "uuid", value = Guid.NewGuid().ToString() });
            case "color":
            {
                var rgb = Random.Shared.Next(0, 0x1000000);
                return ConditionalGetEtag.JsonContent(new { type = "color", value = $"#{rgb:X6}" });
            }
            default:
                return Problem(
                    detail: $"Unsupported type '{type}'. Allowed values: number, string, uuid, color.",
                    statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static string GenerateUrlSafeAlphanumeric(int length)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = UrlSafeAlphabet[Random.Shared.Next(UrlSafeAlphabet.Length)];

        return new string(buffer);
    }
}
