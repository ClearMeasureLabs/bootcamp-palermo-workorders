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
    public void Get_Should_ReturnJsonGreeting_With200Status()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.StatusCode.ShouldBe(200);

        var jsonElement = ((JsonElement)okResult.Value).GetRawText();
        var doc = JsonDocument.Parse(jsonElement);
        var root = doc.RootElement;
        root.TryGetProperty("message", out var messageProp).ShouldBeTrue();
        messageProp.GetString().ShouldBe("Hello, World!");
    }
}
