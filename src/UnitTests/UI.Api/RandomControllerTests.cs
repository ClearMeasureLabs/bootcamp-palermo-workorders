using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public partial class RandomControllerTests
{
    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex AlphanumericRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

    [Test]
    public void Get_WithTypeNumber_Should_ReturnOkJsonWithInteger()
    {
        var controller = CreateController(new Random(42));

        var result = controller.Get("number", null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("number");
        payload.Value.ValueKind.ShouldBe(JsonValueKind.Number);
        payload.Value.GetInt32().ShouldBeInRange(0, 100);
    }

    [Test]
    public void Get_WithTypeNumberAndMinMax_Should_ReturnValueWithinBounds()
    {
        var controller = CreateController(new Random(99));

        var result = controller.Get("number", "50", "150", null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("number");
        var value = payload.Value.GetInt32();
        value.ShouldBeGreaterThanOrEqualTo(50);
        value.ShouldBeLessThanOrEqualTo(150);
    }

    [Test]
    public void Get_WithTypeString_Should_ReturnOkJsonWithAlphanumeric()
    {
        var controller = CreateController(new Random(7));

        var result = controller.Get("string", null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("string");
        var value = payload.Value.GetString();
        value.ShouldNotBeNull();
        AlphanumericRegex().IsMatch(value!).ShouldBeTrue();
        value!.Length.ShouldBe(16);
    }

    [Test]
    public void Get_WithTypeStringAndLength_Should_ReturnValueOfExactLength()
    {
        var controller = CreateController(new Random(11));

        var result = controller.Get("string", null, null, "25", null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("string");
        payload.Value.GetString()!.Length.ShouldBe(25);
    }

    [Test]
    public void Get_WithTypeUuid_Should_ReturnOkJsonWithValidGuidString()
    {
        var controller = CreateController();

        var result = controller.Get("uuid", null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("uuid");
        Guid.TryParse(payload.Value.GetString(), out _).ShouldBeTrue();
    }

    [Test]
    public void Get_WithTypeColor_Should_ReturnOkJsonWithHexColor()
    {
        var controller = CreateController(new Random(3));

        var result = controller.Get("color", null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = DeserializeResponse(content.Content!);
        payload.Type.ShouldBe("color");
        HexColorRegex().IsMatch(payload.Value.GetString()!).ShouldBeTrue();
    }

    [Test]
    public void Get_WithMissingType_Should_ReturnBadRequest()
    {
        var controller = CreateController();

        var result = controller.Get(null, null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(400);
        DeserializeError(content.Content!).Error.ShouldBe("type parameter required");
    }

    [Test]
    public void Get_WithInvalidType_Should_ReturnBadRequest()
    {
        var controller = CreateController();

        var result = controller.Get("invalid", null, null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(400);
        DeserializeError(content.Content!).Error.ShouldContain("number");
        DeserializeError(content.Content!).Error.ShouldContain("uuid");
    }

    [Test]
    public void Get_WithInvalidNumberMin_Should_ReturnBadRequest()
    {
        var controller = CreateController();

        var result = controller.Get("number", "abc", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(400);
        DeserializeError(content.Content!).Error.ShouldContain("min");
    }

    [Test]
    public void Get_WithStringLengthExceedsMax_Should_ReturnBadRequest()
    {
        var controller = CreateController();

        var result = controller.Get("string", null, null, "5000", null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(400);
        DeserializeError(content.Content!).Error.ShouldContain("1000");
    }

    private static RandomController CreateController(Random? random = null) =>
        new(random)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static TestRandomValueResponse DeserializeResponse(string json)
    {
        var payload = JsonSerializer.Deserialize<TestRandomValueResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    private static RandomErrorResponse DeserializeError(string json)
    {
        var payload = JsonSerializer.Deserialize<RandomErrorResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    private sealed record TestRandomValueResponse(string Type, JsonElement Value);
}
