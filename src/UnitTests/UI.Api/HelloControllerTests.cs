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
    public void Test_Get_Should_Return200StatusCode()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.StatusCode.ShouldBe(200);
    }

    [Test]
    public void Test_Get_Should_ReturnJsonContentType()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.ContentType.ShouldNotBeNull();
        result.ContentType.ShouldContain("application/json");
    }

    [Test]
    public void Test_Get_Should_ReturnCorrectMessageStructure()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        result.Value.ShouldNotBeNull();
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("message", out var messageProperty).ShouldBeTrue();
        messageProperty.GetString().ShouldBe("Hello, World!");
    }
}
