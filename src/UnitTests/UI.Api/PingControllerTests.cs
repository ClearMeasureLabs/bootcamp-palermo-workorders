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
        var controller = new PingController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBe("pong");
    }

    [Test]
    public void Head_Should_Return200WithNoBody()
    {
        var controller = new PingController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Head();

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(200);
    }
}
