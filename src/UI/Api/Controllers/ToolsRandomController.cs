using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates random values for integrations and developer tooling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsRandomController : ControllerBase
{
    private const int RandomStringDefaultLength = 16;
    private const string RandomStringCharset =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private const int RandomNumberInclusiveMax = int.MaxValue - 1;

    private const string AllowedTypesDescription =
        "The type query parameter is required and must be one of: number, string, uuid, color (case-insensitive).";

    /// <summary>
    /// Returns a random value of the requested <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>number</b> — inclusive integer from 0 through 2_147_483_646 (<see cref="int.MaxValue"/> minus 1), via <see cref="Random.Shared"/>.</para>
    /// <para><b>string</b> — length 16, characters from A–Z, a–z, and 0–9.</para>
    /// <para><b>uuid</b> — <see cref="Guid"/> in lowercase with hyphens, no braces (standard "D" format).</para>
    /// <para><b>color</b> — CSS hex <c>#RRGGBB</c> using uppercase hexadecimal digits.</para>
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RandomToolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Get([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                detail: AllowedTypesDescription,
                statusCode: StatusCodes.Status400BadRequest);
        }

        switch (type.Trim().ToLowerInvariant())
        {
            case "number":
                var n = Random.Shared.Next(RandomNumberInclusiveMax + 1);
                return ConditionalGetEtag.JsonContent(new RandomToolResponse("number", n));
            case "string":
                var chars = new char[RandomStringDefaultLength];
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = RandomStringCharset[Random.Shared.Next(RandomStringCharset.Length)];
                }

                return ConditionalGetEtag.JsonContent(new RandomToolResponse("string", new string(chars)));
            case "uuid":
                var id = Guid.NewGuid().ToString("D");
                return ConditionalGetEtag.JsonContent(new RandomToolResponse("uuid", id));
            case "color":
                Span<byte> rgb = stackalloc byte[3];
                Random.Shared.NextBytes(rgb);
                var hex = Convert.ToHexString(rgb);
                return ConditionalGetEtag.JsonContent(new RandomToolResponse("color", "#" + hex));
            default:
                return Problem(
                    detail: $"Unknown type '{type.Trim()}'. {AllowedTypesDescription}",
                    statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/random</c> and <c>GET /api/v1.0/tools/random</c>.
/// </summary>
/// <param name="Type">Canonical type key (lowercase).</param>
/// <param name="Value">Generated value; numeric types serialize as JSON numbers.</param>
public record RandomToolResponse(string Type, object Value);
