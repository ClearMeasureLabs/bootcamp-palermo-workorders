using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsWordCountControllerTests
{
    [Test]
    public void Post_Should_ReturnCounts_When_TextProvided()
    {
        var result = CreateController().Post(new WordCountRequest("hello world\nfoo"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.WordCount.ShouldBe(3);
        payload.CharacterCount.ShouldBe(15);
        payload.LineCount.ShouldBe(2);
    }

    [Test]
    public void Post_Should_ReturnZeroWordCount_When_EmptyString()
    {
        var result = CreateController().Post(new WordCountRequest(""));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.WordCount.ShouldBe(0);
        payload.CharacterCount.ShouldBe(0);
        payload.LineCount.ShouldBe(1);
    }

    [Test]
    public void Post_Should_CountSingleLine_When_NoNewlines()
    {
        var result = CreateController().Post(new WordCountRequest("one two three"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.WordCount.ShouldBe(3);
        payload.CharacterCount.ShouldBe(13);
        payload.LineCount.ShouldBe(1);
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissing()
    {
        var nullBody = CreateController().Post(null);
        var nullText = CreateController().Post(new WordCountRequest(null));

        foreach (var result in new[] { nullBody, nullText })
        {
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
            objectResult.Value.ShouldBeOfType<ProblemDetails>();
        }
    }

    [Test]
    public void Post_Should_CountMultipleLines_When_MultipleNewlines()
    {
        var result = CreateController().Post(new WordCountRequest("a\nb\nc"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.WordCount.ShouldBe(3);
        payload.LineCount.ShouldBe(3);
    }

    [Test]
    public void Post_Should_CountParagraphs_When_MultiParagraphText()
    {
        var result = CreateController().Post(new WordCountRequest("first para\n\nsecond para\n\n\nthird"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WordCountResponse>();
        payload.ParagraphCount.ShouldBe(3);
    }

    [Test]
    public void Post_Should_ReturnZeroParagraphCount_When_BlankText()
    {
        var emptyOk = CreateController().Post(new WordCountRequest("")).ShouldBeOfType<OkObjectResult>();
        emptyOk.Value.ShouldBeOfType<WordCountResponse>().ParagraphCount.ShouldBe(0);

        var whitespaceOk = CreateController().Post(new WordCountRequest("  \n\t\n  ")).ShouldBeOfType<OkObjectResult>();
        whitespaceOk.Value.ShouldBeOfType<WordCountResponse>().ParagraphCount.ShouldBe(0);
    }

    private static ToolsWordCountController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
