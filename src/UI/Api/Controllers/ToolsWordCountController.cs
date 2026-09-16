using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Counts words, characters, and lines in submitted text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/word-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/word-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsWordCountController : ControllerBase
{
    /// <summary>
    /// Returns word, character, and line counts for the submitted <paramref name="request"/>.Text.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WordCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] WordCountRequest? request)
    {
        if (request?.Text is null)
        {
            return Problem(
                detail: "JSON body field 'text' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var text = request.Text;
        var wordCount = text.Length == 0
            ? 0
            : text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length;
        var characterCount = text.Length;
        var lineCount = text.Split('\n').Length;

        return Ok(new WordCountResponse(wordCount, characterCount, lineCount));
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountResponse(int WordCount, int CharacterCount, int LineCount);
