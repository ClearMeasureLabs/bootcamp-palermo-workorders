using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HashControllerTests
{
    private static HashController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static HashResponse DeserializeHashResponse(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<HashResponse>();
        return payload;
    }

    [Test]
    public void Post_Should_ReturnSha256Hash_When_RequestValid()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("hello"));

        var payload = DeserializeHashResponse(result);
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Sha256.ShouldMatch("^[0-9a-f]+$");
    }

    [Test]
    public void Post_Should_ReturnAllHashTypes_When_RequestHasText()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("hello"));

        var payload = DeserializeHashResponse(result);
        payload.Sha256.ShouldMatch("^[0-9a-f]+$");
        payload.Md5.ShouldMatch("^[0-9a-f]+$");
        payload.Sha1.ShouldMatch("^[0-9a-f]+$");
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_TextIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest(null));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_TextIsWhitespace()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("   "));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_TextIsMissing()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest(null));

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnBadRequest_When_BodyIsEmpty()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnLowercaseHex_When_Called()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("HashMe"));

        var payload = DeserializeHashResponse(result);
        payload.Sha256.ShouldMatch("^[0-9a-f]+$");
        payload.Md5.ShouldMatch("^[0-9a-f]+$");
        payload.Sha1.ShouldMatch("^[0-9a-f]+$");
    }

    [Test]
    public void Post_Should_ReturnCorrectSha256_When_Called_WithKnownVector()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("test"));

        DeserializeHashResponse(result).Sha256
            .ShouldBe("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
    }

    [Test]
    public void Post_Should_ReturnCorrectMd5_When_Called_WithKnownVector()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("test"));

        DeserializeHashResponse(result).Md5.ShouldBe("098f6bcd4621d373cade4e832627b4f6");
    }

    [Test]
    public void Post_Should_ReturnCorrectSha1_When_Called_WithKnownVector()
    {
        var controller = CreateController();

        var result = controller.Post(new HashRequest("test"));

        DeserializeHashResponse(result).Sha1.ShouldBe("a94a8fe5ccb19ba61c4c0873d391e987982fbbd3");
    }
}
