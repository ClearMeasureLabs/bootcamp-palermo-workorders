using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    private const string UrlSafeAlphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    private static ToolsRandomController CreateController(HttpContext? httpContext = null)
    {
        httpContext ??= new DefaultHttpContext();
        return new ToolsRandomController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Test]
    public void Get_Should_ReturnJsonNumber_WhenTypeIsNumber()
    {
        var controller = CreateController();

        var result = controller.Get("number");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("number");
        doc.RootElement.GetProperty("value").TryGetInt32(out var n).ShouldBeTrue();
        n.ShouldBeGreaterThanOrEqualTo(int.MinValue);
        n.ShouldBeLessThanOrEqualTo(int.MaxValue);
    }

    [Test]
    public void Get_Should_ReturnJsonString_WhenTypeIsString()
    {
        var controller = CreateController();

        var result = controller.Get("string");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("string");
        var text = doc.RootElement.GetProperty("value").GetString();
        text.ShouldNotBeNull();
        text!.Length.ShouldBe(ToolsRandomController.DefaultStringLength);
        text.All(UrlSafeAlphabet.Contains).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnJsonUuid_WhenTypeIsUuid()
    {
        var controller = CreateController();

        var result = controller.Get("uuid");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("uuid");
        var guidText = doc.RootElement.GetProperty("value").GetString();
        Guid.TryParse(guidText, out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnHexColor_WhenTypeIsColor()
    {
        var controller = CreateController();

        var result = controller.Get("color");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("color");
        var hex = doc.RootElement.GetProperty("value").GetString();
        Regex.IsMatch(hex!, "^#[0-9A-F]{6}$", RegexOptions.None).ShouldBeTrue();
    }

    [TestCase("NUMBER")]
    [TestCase("Number")]
    [TestCase("  number  ")]
    public void Get_Should_AcceptTypeCaseInsensitive_AndTrim(string typeArgument)
    {
        var controller = CreateController();

        var result = controller.Get(typeArgument);

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("number");
    }

    [Test]
    public void Get_Should_ReturnBadRequest_WhenTypeMissing()
    {
        var controller = CreateController();

        var result = controller.Get("");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNullOrEmpty();
        problem.Detail.ShouldContain("'type'");
    }

    [Test]
    public void Get_Should_ReturnBadRequest_WhenTypeUnsupported()
    {
        var controller = CreateController();

        var result = controller.Get("boolean");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("boolean");
        problem.Detail.ShouldContain("number");
    }
}
