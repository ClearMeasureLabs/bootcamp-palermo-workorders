using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Utility endpoints for integrations and operators.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Computes the SHA-256 hash of the request body <paramref name="request"/>.<see cref="HashTextRequest.Text"/> using UTF-8 encoding.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(HashTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashTextRequest? request)
    {
        if (request is null)
        {
            return Problem(
                detail: "JSON body is required with a \"text\" property (string).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var text = request.Text ?? string.Empty;
        var utf8 = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(utf8);
        var hex = Convert.ToHexStringLower(hash);
        var payload = new HashTextResponse(Algorithm: "SHA-256", HashHex: hex);
        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/hash</c>.
/// </summary>
public sealed record HashTextRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/hash</c> and <c>POST /api/v1.0/tools/hash</c>.
/// </summary>
public sealed record HashTextResponse(string Algorithm, string HashHex);
