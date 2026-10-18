using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsSlugifyControllerTests
{
    [Test]
    public void Post_Should_ReturnSlug_When_TextProvided()
    {
        var result = CreateController().Post(new SlugifyRequest("Hello World!"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("hello-world");
    }

    [Test]
    public void Post_Should_CollapseNonAlphanumerics_When_MixedSeparators()
    {
        var result = CreateController().Post(new SlugifyRequest("Foo---Bar  Baz"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("foo-bar-baz");
    }

    [Test]
    public void Post_Should_TrimHyphens_When_LeadingAndTrailing()
    {
        var result = CreateController().Post(new SlugifyRequest("--Hello--"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("hello");
    }

    [Test]
    public void Post_Should_ReturnEmptySlug_When_EmptyString()
    {
        var result = CreateController().Post(new SlugifyRequest(""));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SlugifyResponse>();
        payload.Slug.ShouldBe("");
    }

    [Test]
    public void Post_Should_Return400ProblemDetails_When_TextMissing()
    {
        AssertBadRequest(CreateController().Post(null));
        AssertBadRequest(CreateController().Post(new SlugifyRequest(null)));
    }

    private static void AssertBadRequest(IActionResult result)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsSlugifyController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
