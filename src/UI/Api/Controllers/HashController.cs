using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes SHA-256, MD5, and SHA-1 hashes for UTF-8 text input.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class HashController : ControllerBase
{
    /// <summary>
    /// Returns lowercase hex digests for the supplied <paramref name="request"/> text (UTF-8).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashRequest? request)
    {
        if (request is null)
        {
            return Problem(
                detail: "A JSON body with a text field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.Text is null)
        {
            return Problem(
                detail: "The text field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.Text.Length > 0 && string.IsNullOrWhiteSpace(request.Text))
        {
            return Problem(
                detail: "The text field must not be whitespace-only.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(HashComputer.Compute(request.Text));
    }
}
