using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HashControllerTests
{
    [Test]
    public void Should_ReturnOkWithHashes_When_ValidRequest()
    {
        var controller = CreateController();
        var expected = TextHasher.ComputeHashes("hello");

        var result = controller.Post(new HashTextRequest("hello"));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<HashTextResponse>();
        payload.ShouldBe(expected);
    }

    [Test]
    public void Should_ReturnBadRequest_When_RequestIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Should_ReturnBadRequest_When_TextIsEmpty()
    {
        var controller = CreateController();

        var result = controller.Post(new HashTextRequest(""));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Should_ReturnBadRequest_When_TextIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(new HashTextRequest(null!));

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public void Should_ReturnOk_When_TextIsWhitespace()
    {
        var controller = CreateController();
        var expected = TextHasher.ComputeHashes(" ");

        var result = controller.Post(new HashTextRequest(" "));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBeOfType<HashTextResponse>().ShouldBe(expected);
    }

    private static HashController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
