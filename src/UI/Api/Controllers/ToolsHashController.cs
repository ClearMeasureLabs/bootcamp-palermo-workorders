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
/// Computes cryptographic digests of arbitrary text (UTF-8).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Returns the SHA-256 hash of the request <c>text</c>. When <c>includeLegacyHashes</c> is true,
    /// also returns MD5 and SHA-1 digests (hexadecimal, lowercase).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ToolsHashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] ToolsHashRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "A non-null \"text\" field is required in the JSON body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var utf8 = Encoding.UTF8.GetBytes(request.Text);
        var sha256Hex = Convert.ToHexStringLower(SHA256.HashData(utf8));

        if (!request.IncludeLegacyHashes)
        {
            return Ok(new ToolsHashResponse { Sha256 = sha256Hex });
        }

        var md5Hex = Convert.ToHexStringLower(MD5.HashData(utf8));
        var sha1Hex = Convert.ToHexStringLower(SHA1.HashData(utf8));
        return Ok(new ToolsHashResponse
        {
            Sha256 = sha256Hex,
            Md5 = md5Hex,
            Sha1 = sha1Hex
        });
    }
}
