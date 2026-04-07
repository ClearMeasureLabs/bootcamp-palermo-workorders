using System.Text.Json.Nodes;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class RandomToolsControllerTests
{
    [Test]
    public void Get_Should_ReturnBadRequest_When_TypeMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_TypeUnknown()
    {
        var controller = CreateController();

        var result = controller.Get("bogus");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnJsonNumber_When_TypeNumber()
    {
        var controller = CreateController();
        var sawNonNegative = false;
        var sawNegative = false;

        for (var i = 0; i < 256; i++)
        {
            var payload = AssertJsonPayload(controller.Get("number"));
            payload["type"]!.GetValue<string>().ShouldBe("number");
            var value = payload["value"]!.GetValue<int>();
            if (value >= 0)
                sawNonNegative = true;
            if (value < 0)
                sawNegative = true;
        }

        sawNonNegative.ShouldBeTrue();
        sawNegative.ShouldBeTrue("Expected at least one negative sample across full int32 range");
    }

    [Test]
    public void Get_Should_ReturnJsonString_When_TypeString()
    {
        var controller = CreateController();

        var result = controller.Get("string");

        var payload = AssertJsonPayload(result);
        payload["type"]!.GetValue<string>().ShouldBe("string");
        var s = payload["value"]!.GetValue<string>();
        s.Length.ShouldBe(16);
        s.ToCharArray().All(c => char.IsAsciiLetterOrDigit(c)).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnJsonUuid_When_TypeUuid()
    {
        var controller = CreateController();

        var result = controller.Get("UUID");

        var payload = AssertJsonPayload(result);
        payload["type"]!.GetValue<string>().ShouldBe("uuid");
        Guid.TryParse(payload["value"]!.GetValue<string>(), out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnJsonColor_When_TypeColor()
    {
        var controller = CreateController();

        var result = controller.Get("color");

        var payload = AssertJsonPayload(result);
        payload["type"]!.GetValue<string>().ShouldBe("color");
        var hex = payload["value"]!.GetValue<string>();
        hex.Length.ShouldBe(7);
        hex[0].ShouldBe('#');
        hex[1..].ToCharArray().All(Uri.IsHexDigit).ShouldBeTrue();
    }

    private static RandomToolsController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static JsonObject AssertJsonPayload(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var node = JsonNode.Parse(content.Content!)!.AsObject();
        return node;
    }
}
