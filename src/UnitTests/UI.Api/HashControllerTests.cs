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

    [Test]
    public void Post_Should_Return200WithKnownHashes_When_TextIsHello()
    {
        var controller = CreateController();
        var request = new HashTextRequest("hello");

        var result = controller.Post(request);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var body = ok.Value.ShouldBeOfType<HashTextResponse>();
        body.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        body.Md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        body.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public void Post_Should_Return200WithEmptyStringHashes_When_TextIsEmpty()
    {
        var controller = CreateController();
        var request = new HashTextRequest(string.Empty);

        var result = controller.Post(request);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var body = ok.Value.ShouldBeOfType<HashTextResponse>();
        body.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        body.Md5.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
        body.Sha1.ShouldBe("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }

    [Test]
    public void Post_Should_Return400Problem_When_TextIsNull()
    {
        var controller = CreateController();
        var request = new HashTextRequest(null);

        var result = controller.Post(request);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("text");
    }

    [Test]
    public void Post_Should_Return400Problem_When_TextIsMissing()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("text");
    }
}
