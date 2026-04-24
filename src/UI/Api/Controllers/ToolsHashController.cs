using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes SHA-256, MD5, and SHA-1 digests of request <c>text</c> using UTF-8 encoding.
/// Success JSON uses stable field names: <c>sha256</c>, <c>md5</c>, <c>sha1</c> (lowercase hex strings).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsHashController : ControllerBase
{
    private const int MaxTextLength = 1024 * 1024;

    /// <summary>
    /// Returns hexadecimal digests of the UTF-8 encoding of <paramref name="request"/>.<see cref="ToolsHashRequest.Text"/>.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxTextLength + 64)]
    [Consumes(MediaTypeNames.Application.Json)]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ToolsHashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] ToolsHashRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body must include a non-null \"text\" property.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.Text.Length > MaxTextLength)
        {
            return Problem(
                detail: $"The \"text\" value must be at most {MaxTextLength} characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var utf8 = Encoding.UTF8.GetBytes(request.Text);
        var sha256Hex = Convert.ToHexString(SHA256.HashData(utf8)).ToLowerInvariant();
        var md5Hex = Convert.ToHexString(MD5.HashData(utf8)).ToLowerInvariant();
        var sha1Hex = Convert.ToHexString(SHA1.HashData(utf8)).ToLowerInvariant();

        return Ok(new ToolsHashResponse
        {
            Sha256 = sha256Hex,
            Md5 = md5Hex,
            Sha1 = sha1Hex
        });
    }
}
