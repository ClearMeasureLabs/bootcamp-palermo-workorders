using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes SHA-256, MD5, and SHA-1 digests of UTF-8 text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Returns lowercase hexadecimal SHA-256, MD5, and SHA-1 digests of the request <c>text</c> field (UTF-8 encoded).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(1 * 1024 * 1024)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(HashTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashTextRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return Problem(
                detail: "A non-empty text field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var (sha256, md5, sha1) = TextHashComputer.Compute(request.Text);
        return Ok(new HashTextResponse(sha256, md5, sha1));
    }
}
