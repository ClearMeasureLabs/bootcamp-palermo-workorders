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
    public void Get_Should_ReturnJsonGreeting()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);

        var payload = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value)).RootElement;
        payload.TryGetProperty("message", out var messageElement).ShouldBeTrue();
        messageElement.GetString().ShouldBe("Hello, World!");
    }
}
