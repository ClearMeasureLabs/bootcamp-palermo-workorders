using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsHashControllerTests
{
    [Test]
    public void ShouldReturnBadRequest_When_RequestNull()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void ShouldReturnBadRequest_When_TextMissingOrWhitespace()
    {
        var controller = CreateController();

        foreach (var text in new[] { "", "   ", "\t\n" })
        {
            var result = controller.Post(new HashTextRequest(text));
            var objectResult = result.ShouldBeOfType<ObjectResult>();
            objectResult.StatusCode.ShouldBe(400);
        }
    }

    [Test]
    public void ShouldReturnOkWithHashes_When_TextProvided()
    {
        var controller = CreateController();

        var result = controller.Post(new HashTextRequest("hello"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<HashTextResponse>();
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        payload.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    private static ToolsHashController CreateController()
    {
        return new ToolsHashController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
