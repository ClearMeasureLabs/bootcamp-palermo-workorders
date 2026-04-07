using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates sample random values for scripts and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/random")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/random")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class RandomToolsController : ControllerBase
{
    private const string AllowedTypesMessage =
        "Query parameter 'type' is required and must be one of: number, string, uuid, color.";

    /// <summary>
    /// Returns one random value according to <paramref name="type"/>.
    /// For <c>type=number</c>, <c>value</c> is a uniform random signed 32-bit integer in <c>[int.MinValue, int.MaxValue]</c>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return Problem(detail: AllowedTypesMessage, statusCode: StatusCodes.Status400BadRequest);

        if (type.Equals("number", StringComparison.OrdinalIgnoreCase))
        {
            var value = (int)Random.Shared.NextInt64(int.MinValue, (long)int.MaxValue + 1);
            var payload = new RandomValueResponse("number", value);
            return JsonWithConditionalGet(payload);
        }

        if (type.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Span<char> buffer = stackalloc char[16];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            var payload = new RandomValueResponse("string", new string(buffer));
            return JsonWithConditionalGet(payload);
        }

        if (type.Equals("uuid", StringComparison.OrdinalIgnoreCase))
        {
            var payload = new RandomValueResponse("uuid", Guid.NewGuid().ToString("D"));
            return JsonWithConditionalGet(payload);
        }

        if (type.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            var n = Random.Shared.Next(0, 0x1000000);
            var payload = new RandomValueResponse("color", $"#{n:X6}");
            return JsonWithConditionalGet(payload);
        }

        return Problem(
            detail: $"Unknown type '{type}'. {AllowedTypesMessage}",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult JsonWithConditionalGet(RandomValueResponse payload)
    {
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON body for <c>GET /api/tools/random</c>.
/// </summary>
public sealed record RandomValueResponse(string Type, object Value);
