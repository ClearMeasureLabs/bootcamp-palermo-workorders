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
    public void Get_Should_ReturnOkWithHelloWorldMessage()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var response = ok.Value.ShouldBeOfType<HelloResponse>();
        response.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public void Get_Should_SerializeMessagePropertyAsCamelCase()
    {
        var response = new HelloResponse("Hello, World!");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(response, options);
        json.ShouldContain("\"message\"");
        json.IndexOf("\"Message\":", StringComparison.Ordinal).ShouldBe(-1);
    }
}
