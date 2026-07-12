using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes a stateless hash utility for computing SHA-256, MD5, and SHA-1 digests of UTF-8 text.
/// When API key authentication is enabled, callers must supply <c>X-API-Key</c> (unlike ping/time/version).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HashController : ControllerBase
{
    /// <summary>
    /// Returns lowercase hexadecimal SHA-256, MD5, and SHA-1 digests of the submitted <c>text</c> field (UTF-8).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(HashTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashTextRequest? request)
    {
        if (request is null || request.Text is null || request.Text.Length == 0)
        {
            return Problem(
                detail: "A non-empty text field is required in the JSON request body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(TextHasher.ComputeHashes(request.Text));
    }
}
