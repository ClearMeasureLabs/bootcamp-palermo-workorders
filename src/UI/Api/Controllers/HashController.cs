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
/// Computes SHA-256, MD5, and SHA-1 hashes of UTF-8 text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/hash")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/hash")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class HashController : ControllerBase
{
    /// <summary>
    /// Returns lowercase hex digests for the UTF-8 encoding of <paramref name="request"/>.<see cref="HashTextRequest.Text"/>.
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
            return Problem(
                detail: "A non-null text field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.Text) && request.Text.Length > 0)
        {
            return Problem(
                detail: "text must not be whitespace-only.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var bytes = Encoding.UTF8.GetBytes(request.Text);
        var payload = new HashTextResponse(
            Sha256: Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Md5: Convert.ToHexStringLower(MD5.HashData(bytes)),
            Sha1: Convert.ToHexStringLower(SHA1.HashData(bytes)));

        return ConditionalGetEtag.JsonContent(payload);
    }
}
