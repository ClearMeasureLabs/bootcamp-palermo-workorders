using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonWithHelloWorldMessage()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.ShouldBeOfType<JsonResult>();
        result.StatusCode.ShouldBe(200);
        var value = result.Value.ShouldNotBeNull();
        var message = value.GetType().GetProperty("message")?.GetValue(value)?.ToString();
        message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_ReturnHttp200StatusCode()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.StatusCode.ShouldBe(200);
    }

    [Test]
    public void Get_Should_ReturnJsonContentType()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.ContentType.ShouldNotBeNull();
        result.ContentType!.ShouldContain("application/json");
    }
}
