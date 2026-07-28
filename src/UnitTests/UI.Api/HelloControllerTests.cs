using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_With_CorrectJson()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);
        var payload = okResult.Value.ShouldBeOfType<HelloResponse>();
        payload.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_SerializeToExpectedJsonShape()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var json = JsonSerializer.Serialize(okResult.Value);
        json.ShouldContain("\"message\":\"Hello, World!\"");
    }
}
