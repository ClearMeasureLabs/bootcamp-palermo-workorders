using System.Net.Mime;
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
public sealed class HashController : ControllerBase
{
    /// <summary>
    /// Returns SHA-256, MD5, and SHA-1 hashes of the request <paramref name="request"/> text (UTF-8).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HashTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashTextRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(detail: "text is required.", statusCode: StatusCodes.Status400BadRequest);
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
/// JSON body for <see cref="HashController.Post"/>.
/// </summary>
/// <param name="Text">Plain text to hash (UTF-8 encoded).</param>
public sealed record HashTextRequest(string? Text);

/// <summary>
/// JSON response containing lowercase hexadecimal digests.
/// </summary>
/// <param name="Sha256">SHA-256 digest.</param>
/// <param name="Md5">MD5 digest.</param>
/// <param name="Sha1">SHA-1 digest.</param>
public sealed record HashTextResponse(string Sha256, string Md5, string Sha1);
