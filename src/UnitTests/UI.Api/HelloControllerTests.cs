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

        var content = result.ShouldBeOfType<OkObjectResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);

        var payload = JsonSerializer.Deserialize<HelloGreeting>(
            JsonSerializer.Serialize(content.Value));
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    private record HelloGreeting(string Message);
}
