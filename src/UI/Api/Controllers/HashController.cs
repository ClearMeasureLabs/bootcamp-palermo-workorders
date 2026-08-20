using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Computes cryptographic hashes of text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HashController : ControllerBase
{
    /// <summary>
    /// Returns the SHA-256 hash of <paramref name="request"/>.<c>text</c>, with optional MD5 and SHA-1.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(detail: "The 'text' field is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var utf8Bytes = Encoding.UTF8.GetBytes(request.Text);
        var response = new HashResponse
        {
            Sha256 = Convert.ToHexString(SHA256.HashData(utf8Bytes)).ToLowerInvariant()
        };

        if (request.IncludeMd5)
        {
            response.Md5 = Convert.ToHexString(MD5.HashData(utf8Bytes)).ToLowerInvariant();
        }

        if (request.IncludeSha1)
        {
            response.Sha1 = Convert.ToHexString(SHA1.HashData(utf8Bytes)).ToLowerInvariant();
        }

        return Ok(response);
    }
}

/// <summary>
/// Request body for <see cref="HashController.Post"/>.
/// </summary>
public sealed class HashRequest
{
    /// <summary>
    /// Text to hash (UTF-8). Required; empty string is allowed.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// When true, the response includes an MD5 hex digest.
    /// </summary>
    public bool IncludeMd5 { get; set; }

    /// <summary>
    /// When true, the response includes a SHA-1 hex digest.
    /// </summary>
    public bool IncludeSha1 { get; set; }
}

/// <summary>
/// Hash digests for the submitted text.
/// </summary>
public sealed class HashResponse
{
    /// <summary>
    /// Lowercase hex SHA-256 digest.
    /// </summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; set; }

    /// <summary>
    /// Lowercase hex MD5 digest when requested; omitted otherwise.
    /// </summary>
    [JsonPropertyName("md5")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Md5 { get; set; }

    /// <summary>
    /// Lowercase hex SHA-1 digest when requested; omitted otherwise.
    /// </summary>
    [JsonPropertyName("sha1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha1 { get; set; }
}
