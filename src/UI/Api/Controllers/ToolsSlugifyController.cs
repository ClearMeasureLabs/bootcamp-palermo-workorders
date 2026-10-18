using System.Net.Mime;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Turns text into a URL slug for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/slugify")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/slugify")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsSlugifyController : ControllerBase
{
    /// <summary>
    /// Returns a URL slug for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(SlugifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] SlugifyRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body field 'text' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new SlugifyResponse(ToSlug(request.Text)));
    }

    private static string ToSlug(string text)
    {
        var sb = new StringBuilder(text.Length);
        var pendingHyphen = false;

        foreach (var c in text.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(c);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/slugify</c>.
/// </summary>
public record SlugifyRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/slugify</c>.
/// </summary>
public record SlugifyResponse(string Slug);
