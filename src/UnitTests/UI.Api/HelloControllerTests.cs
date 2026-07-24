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
    public void Should_Return200WithJsonContent_When_Get()
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
        json.ShouldContain("\"message\":\"Hello, World!\"");
    }

    [Test]
    public void Should_ReturnOkResult_When_ControllerInvoked()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.ShouldBeOfType<OkObjectResult>();
    }
}
