using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes cryptographic digests of a UTF-8 string for scripts and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Returns SHA-256 (and optionally MD5 and SHA-1) of the request <c>text</c> as UTF-8 bytes.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes(MediaTypeNames.Application.Json)]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ToolsHashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public IActionResult Post([FromBody] ToolsHashRequest? request)
    {
        if (request is null)
        {
            return Problem(
                detail: "A JSON body with a text property is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var text = request.Text!;
        var response = ToolsHashDigest.Compute(text, request.IncludeLegacyHashes);
        return Ok(response);
    }
}
