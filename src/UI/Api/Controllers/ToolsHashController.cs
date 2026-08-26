using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes cryptographic hashes of text input for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Returns the SHA-256 hash of <paramref name="request"/>.Text (UTF-8),
    /// optionally including MD5 and/or SHA-1 when requested via body or query flags.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post(
        [FromBody] HashRequest? request,
        [FromQuery] bool? includeMd5 = null,
        [FromQuery] bool? includeSha1 = null)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body field 'text' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var bytes = Encoding.UTF8.GetBytes(request.Text);
        var wantMd5 = includeMd5 ?? request.IncludeMd5;
        var wantSha1 = includeSha1 ?? request.IncludeSha1;

        return Ok(new HashResponse(
            Sha256: ToLowerHex(SHA256.HashData(bytes)),
            Md5: wantMd5 ? ToLowerHex(ComputeMd5(bytes)) : null,
            Sha1: wantSha1 ? ToLowerHex(ComputeSha1(bytes)) : null));
    }

    private static string ToLowerHex(byte[] hash) =>
        Convert.ToHexString(hash).ToLowerInvariant();

#pragma warning disable CA5351 // MD5/SHA-1 exposed intentionally for optional legacy hash utility
    private static byte[] ComputeMd5(byte[] bytes) => MD5.HashData(bytes);

    private static byte[] ComputeSha1(byte[] bytes) => SHA1.HashData(bytes);
#pragma warning restore CA5351
}

/// <summary>
/// Request body for <c>POST /api/tools/hash</c>.
/// </summary>
public record HashRequest(string? Text, bool IncludeMd5 = false, bool IncludeSha1 = false);

/// <summary>
/// JSON payload for <c>POST /api/tools/hash</c>. Optional algorithms are omitted when not requested.
/// </summary>
public record HashResponse(
    string Sha256,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Md5 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Sha1 = null);
