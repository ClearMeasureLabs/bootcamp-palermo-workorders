using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    [Test]
    public void Get_Should_ReturnIntInRange()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(kind: "int", min: "10", max: "20");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("kind").GetString().ShouldBe("int");
        doc.RootElement.GetProperty("minInclusive").GetInt32().ShouldBe(10);
        doc.RootElement.GetProperty("maxExclusive").GetInt32().ShouldBe(20);
        var v = doc.RootElement.GetProperty("value").GetInt32();
        v.ShouldBeGreaterThanOrEqualTo(10);
        v.ShouldBeLessThan(20);
    }

    [Test]
    public void Get_Should_Return400_WhenKindUnknown()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(kind: "widget");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return400_WhenBytesEncodingUnknown()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(kind: "bytes", length: "4", encoding: "rot13");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnGuidShape_WhenKindGuid()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(kind: "guid");

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("kind").GetString().ShouldBe("guid");
        Guid.TryParse(doc.RootElement.GetProperty("value").GetString(), out var g).ShouldBeTrue();
        g.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Get_Should_ReturnStringOfRequestedLength()
    {
        var controller = new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(kind: "string", length: "12", charset: "AB");

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("length").GetInt32().ShouldBe(12);
        doc.RootElement.GetProperty("value").GetString()!.Length.ShouldBe(12);
    }
}
