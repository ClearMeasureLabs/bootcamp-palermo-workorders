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
        var words = text.Length == 0
            ? []
            : text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var characterCount = text.Length;
        // Normalize CRLF to LF, then split on LF to count lines (handles CR, LF, and CRLF).
        var lineCount = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Length;
        var longestWord = FindLongestWord(words);

        return Ok(new WordCountResponse(wordCount, characterCount, lineCount, longestWord));
    }

    private static string FindLongestWord(string[] words)
    {
        if (words.Length == 0)
        {
            return "";
        }

        var longest = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            if (words[i].Length > longest.Length)
            {
                longest = words[i];
            }
        }

        return longest;
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountResponse(int WordCount, int CharacterCount, int LineCount, string LongestWord);
