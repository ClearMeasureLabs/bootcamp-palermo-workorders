using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/word-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/word-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsWordCountController : ControllerBase
{
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
        var lineCount = CountLinesTreatingCrLfCrAndLfAsLineBreaks(text);
        var sentenceCount = CountSentencesEndingInPeriodExclamationOrQuestion(text);

        return Ok(new WordCountResponse(wordCount, characterCount, lineCount, sentenceCount));
    }

    private static int CountLinesTreatingCrLfCrAndLfAsLineBreaks(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Length;

    private static int CountSentencesEndingInPeriodExclamationOrQuestion(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        return CountTerminatorsAfterContent(text);
    }

    private static int CountTerminatorsAfterContent(string text)
    {
        var count = 0;
        var hasContent = false;
        foreach (var c in text)
        {
            ApplySentenceChar(c, ref hasContent, ref count);
        }

        return count;
    }

    private static void ApplySentenceChar(char c, ref bool hasContent, ref int count)
    {
        if (c is '.' or '!' or '?')
        {
            if (hasContent)
            {
                count++;
                hasContent = false;
            }

            return;
        }

        if (!char.IsWhiteSpace(c))
        {
            hasContent = true;
        }
    }
}

public record WordCountRequest(string? Text);

public record WordCountResponse(int WordCount, int CharacterCount, int LineCount, int SentenceCount);
