using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithHelloWorldMessage()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var payload = ok.Value.ShouldBeOfType<HelloResponse>();
        payload.Message.ShouldBe("Hello, World!");

        var json = JsonSerializer.Serialize(payload, ConditionalGetEtag.JsonSerializerOptions);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }
}
