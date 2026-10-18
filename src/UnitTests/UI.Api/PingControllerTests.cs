using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class PingControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextPong()
    {
        var controller = CreateController();

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBe("pong");
    }

    [Test]
    public void Get_Should_SetCacheControlNoStore()
    {
        var controller = CreateController();

        controller.Get();

        controller.Response.Headers.CacheControl.ToString().ShouldBe("no-store");
    }

    private static PingController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
