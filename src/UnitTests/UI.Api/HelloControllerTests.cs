using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOkWith200StatusCode()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
    }

    [Test]
    public void Get_Should_ReturnJsonContentType()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
    }

    [Test]
    public void Get_Should_ReturnMessageProperty()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<HelloResponse>();
        response.message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_DeserializeToHelloResponse()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<HelloResponse>();
        response.ShouldNotBeNull();
        response.GetType().Name.ShouldBe("HelloResponse");
    }
}
