using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    private static JsonSerializerOptions JsonOptions => ConditionalGetEtag.JsonSerializerOptions;

    [Test]
    public void Get_Should_Return200_WithValidJsonShape_When_TypeIsNumber()
    {
        var controller = CreateController();

        var result = controller.Get("number");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<JsonElement>(content.Content!, JsonOptions);
        payload.GetProperty("type").GetString().ShouldBe("number");
        var n = payload.GetProperty("value").GetInt32();
        n.ShouldBeGreaterThanOrEqualTo(0);
        n.ShouldBeLessThanOrEqualTo(int.MaxValue - 1);
    }

    [Test]
    public void Get_Should_Return200_WithValidJsonShape_When_TypeIsString()
    {
        var controller = CreateController();

        var result = controller.Get("string");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = JsonSerializer.Deserialize<JsonElement>(content.Content!, JsonOptions);
        payload.GetProperty("type").GetString().ShouldBe("string");
        var s = payload.GetProperty("value").GetString();
        s.ShouldNotBeNull();
        s!.Length.ShouldBe(16);
        Regex.IsMatch(s, @"^[A-Za-z0-9]{16}$").ShouldBeTrue();
    }

    [Test]
    public void Get_Should_Return200_WithValidJsonShape_When_TypeIsUuid()
    {
        var controller = CreateController();

        var result = controller.Get("uuid");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = JsonSerializer.Deserialize<JsonElement>(content.Content!, JsonOptions);
        payload.GetProperty("type").GetString().ShouldBe("uuid");
        var s = payload.GetProperty("value").GetString();
        s.ShouldNotBeNull();
        Guid.TryParse(s, out _).ShouldBeTrue();
        s!.ShouldNotContain("{");
    }

    [Test]
    public void Get_Should_Return200_WithValidJsonShape_When_TypeIsColor()
    {
        var controller = CreateController();

        var result = controller.Get("color");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = JsonSerializer.Deserialize<JsonElement>(content.Content!, JsonOptions);
        payload.GetProperty("type").GetString().ShouldBe("color");
        var s = payload.GetProperty("value").GetString();
        s.ShouldNotBeNull();
        Regex.IsMatch(s!, "^#[0-9A-F]{6}$").ShouldBeTrue();
    }

    [Test]
    public void Get_Should_Return400_When_TypeIsMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        AssertProblem400(result);
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("foo")]
    public void Get_Should_Return400_When_TypeIsEmptyOrUnknown(string? type)
    {
        var controller = CreateController();

        var result = controller.Get(type);

        AssertProblem400(result);
    }

    [Test]
    [TestCase("NUMBER")]
    [TestCase("Uuid")]
    [TestCase("CoLoR")]
    public void Get_Should_AcceptTypeCaseInsensitively_When_TypeVariesByCasing(string type)
    {
        var controller = CreateController();

        var result = controller.Get(type);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
    }

    private static ToolsRandomController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static void AssertProblem400(IActionResult result)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var details = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
        details.Detail!.ShouldContain("number");
        details.Detail.ShouldContain("string");
        details.Detail.ShouldContain("uuid");
        details.Detail.ShouldContain("color");
    }
}
