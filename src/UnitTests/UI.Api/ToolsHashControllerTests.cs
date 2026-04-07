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
    public void Post_Should_Return400_When_RequestIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_Return400_When_TextIsNull()
    {
        var controller = CreateController();

        var result = controller.Post(new ToolsHashRequest(null));

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Post_Should_ReturnSha256Only_When_LegacyNotRequested()
    {
        var controller = CreateController();

        var result = controller.Post(new ToolsHashRequest("hello", IncludeLegacyHashes: false));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ToolsHashResponse>();
        payload.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        payload.Md5.ShouldBeNull();
        payload.Sha1.ShouldBeNull();
    }

    [Test]
    public void Post_Should_ReturnEmptyStringDigests_When_TextIsEmpty()
    {
        var controller = CreateController();

        var result = controller.Post(new ToolsHashRequest("", IncludeLegacyHashes: true));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ToolsHashResponse>();
        payload.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        payload.Md5.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
        payload.Sha1.ShouldBe("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }

    [Test]
    public void Post_Should_ReturnLegacyHashes_When_Requested()
    {
        var controller = CreateController();

        var result = controller.Post(new ToolsHashRequest("abc", IncludeLegacyHashes: true));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<ToolsHashResponse>();
        payload.Sha256.ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        payload.Md5.ShouldBe("900150983cd24fb0d6963f7d28e17f72");
        payload.Sha1.ShouldBe("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    private static ToolsHashController CreateController()
    {
        return new ToolsHashController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
