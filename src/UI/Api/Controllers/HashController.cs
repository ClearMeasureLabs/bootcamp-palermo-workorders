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
/// Computes cryptographic hashes of UTF-8 text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HashController : ControllerBase
{
    /// <summary>
    /// Returns SHA-256, MD5, and SHA-1 hashes of the request text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    public ActionResult<HashTextResponse> Post([FromBody] HashTextRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "text is required");
        }

        var bytes = Encoding.UTF8.GetBytes(request.Text);
        var response = new HashTextResponse(
            Sha256: Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Md5: Convert.ToHexStringLower(MD5.HashData(bytes)),
            Sha1: Convert.ToHexStringLower(SHA1.HashData(bytes)));

        return Ok(response);
    }
}

/// <summary>
/// Request body for hash computation.
/// </summary>
/// <param name="Text">UTF-8 text to hash.</param>
public record HashTextRequest(string? Text);

/// <summary>
/// Hash digests as lowercase hexadecimal strings.
/// </summary>
/// <param name="Sha256">SHA-256 digest.</param>
/// <param name="Md5">MD5 digest.</param>
/// <param name="Sha1">SHA-1 digest.</param>
public record HashTextResponse(string Sha256, string Md5, string Sha1);
