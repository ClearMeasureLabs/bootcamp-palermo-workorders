using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes SHA-256 (primary digest), MD5, and SHA-1 hex digests over UTF-8 bytes of supplied text.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class ToolsHashController : ControllerBase
{
    /// <summary>
    /// Accepts JSON <c>{ "text": "..." }</c> and returns lowercase hexadecimal <c>sha256</c>, <c>md5</c>, and <c>sha1</c> digests computed from UTF-8 encoding (no BOM, no normalization).
    /// </summary>
    [HttpPost]
    public ActionResult<HashTextResponse> Post(HashTextRequest request)
    {
        var bytes = Encoding.UTF8.GetBytes(request.Text);
        var sha256Hex = HexLower(SHA256.HashData(bytes));
        var md5Hex = HexLower(MD5.HashData(bytes));
#pragma warning disable CA5350 // SHA-1 only for interoperability / checksum per API contract — not security
        var sha1Hex = HexLower(SHA1.HashData(bytes));
#pragma warning restore CA5350

        var response = new HashTextResponse(sha256Hex, md5Hex, sha1Hex);
        return Ok(response);
    }

    private static string HexLower(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// JSON body for POST <c>/api/tools/hash</c>.
/// </summary>
public sealed record HashTextRequest
{
    /// <summary>The input string hashed as UTF-8 (including empty).</summary>
    public required string Text { get; init; }
}

/// <summary>
/// Response digest payload (all hex strings lowercase).
/// </summary>
public sealed record HashTextResponse(string Sha256, string Md5, string Sha1);
