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
/// Computes cryptographic digests of UTF-8 text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HashController : ControllerBase
{
    /// <summary>
    /// Returns SHA-256, MD5, and SHA-1 hex digests of the request <c>text</c> field (UTF-8 encoded).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(HashResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] HashRequest? request)
    {
        if (request?.Text is not { Length: > 0 } text || string.IsNullOrWhiteSpace(text))
        {
            return Problem(
                detail: "A non-empty text field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var utf8 = Encoding.UTF8.GetBytes(text);
        var payload = new HashResponse(
            Sha256: Convert.ToHexStringLower(SHA256.HashData(utf8)),
            Md5: Convert.ToHexStringLower(MD5.HashData(utf8)),
            Sha1: Convert.ToHexStringLower(SHA1.HashData(utf8)));

        return ConditionalGetEtag.JsonContent(payload);
    }
}

/// <summary>
/// JSON body for <c>POST /api/tools/hash</c>.
/// </summary>
public record HashRequest(string Text);

/// <summary>
/// JSON payload with lowercase-hex digests of the submitted text (UTF-8).
/// </summary>
public record HashResponse(string Sha256, string Md5, string Sha1);
