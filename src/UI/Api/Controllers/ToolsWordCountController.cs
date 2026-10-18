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
    /// Returns word, character, line, and sentence counts for the submitted <paramref name="request"/>.Text.
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
        // Normalize CRLF to LF, then split on LF to count lines (handles CR, LF, and CRLF).
        var lineCount = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Length;
        var sentenceCount = CountSentences(text);

        return Ok(new WordCountResponse(wordCount, characterCount, lineCount, sentenceCount));
    }

    /// <summary>
    /// Counts runs of text that end in <c>.</c>, <c>!</c>, or <c>?</c>; blank text is 0.
    /// </summary>
    private static int CountSentences(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var count = 0;
        var start = 0;
        while (start < text.Length)
        {
            var end = text.IndexOfAny(['.', '!', '?'], start);
            if (end < 0)
            {
                break;
            }

            if (HasNonWhitespace(text, start, end))
            {
                count++;
            }

            start = end + 1;
        }

        return count;
    }

    private static bool HasNonWhitespace(string text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountRequest(string? Text);

/// <summary>
/// JSON payload for <c>POST /api/tools/word-count</c>.
/// </summary>
public record WordCountResponse(int WordCount, int CharacterCount, int LineCount, int SentenceCount);
