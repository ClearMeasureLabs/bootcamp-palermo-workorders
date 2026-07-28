using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomControllerTests
{
    [Test]
    public void Get_WithTypeNumber_Should_ReturnPlainTextNumber()
    {
        var controller = CreateController(new StubRandom(42));

        var result = controller.Get("number", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        var value = int.Parse(content.Content!, CultureInfo.InvariantCulture);
        value.ShouldBeInRange(0, 99);
    }

    [Test]
    public void Get_WithTypeNumber_And_CustomBounds_Should_ReturnNumberInRange()
    {
        var controller = CreateController(new StubRandom(7));

        var result = controller.Get("number", 10, 20, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var value = int.Parse(content.Content!, CultureInfo.InvariantCulture);
        value.ShouldBeGreaterThanOrEqualTo(10);
        value.ShouldBeLessThan(20);
    }

    [Test]
    public void Get_WithTypeNumber_And_InvalidBounds_Should_Return400()
    {
        var controller = CreateController(new StubRandom(1));

        var result = controller.Get("number", 100, 50, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_WithTypeString_Should_ReturnPlainTextAlphanumeric()
    {
        var controller = CreateController(new StubRandom(12345));

        var result = controller.Get("string", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content!.Length.ShouldBe(16);
        Regex.IsMatch(content.Content, "^[a-zA-Z0-9]+$").ShouldBeTrue();
    }

    [Test]
    public void Get_WithTypeString_And_CustomLength_Should_ReturnStringOfLength()
    {
        var controller = CreateController(new StubRandom(99));

        var result = controller.Get("string", null, null, 8);

        var content = result.ShouldBeOfType<ContentResult>();
        content.Content!.Length.ShouldBe(8);
    }

    [Test]
    public void Get_WithTypeString_And_InvalidLength_Should_Return400()
    {
        var controller = CreateController(new StubRandom(1));

        controller.Get("string", null, null, 0).ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
        controller.Get("string", null, null, -1).ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
        controller.Get("string", null, null, 257).ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_WithTypeUuid_Should_ReturnPlainTextGuid()
    {
        var controller = CreateController(new StubRandom(1));

        var result = controller.Get("uuid", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        Guid.TryParse(content.Content, out _).ShouldBeTrue();
    }

    [Test]
    public void Get_WithTypeColor_Should_ReturnHexColorCode()
    {
        var controller = CreateController(new StubRandom(0xAABBCC));

        var result = controller.Get("color", null, null, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        Regex.IsMatch(content.Content!, "^#[0-9A-Fa-f]{6}$").ShouldBeTrue();
    }

    [Test]
    public void Get_WithMissingType_Should_Return400()
    {
        var controller = CreateController(new StubRandom(1));

        var result = controller.Get(null, null, null, null);

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_WithInvalidType_Should_Return400()
    {
        var controller = CreateController(new StubRandom(1));

        var result = controller.Get("invalid", null, null, null);

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_WithTypeIgnoreCase_Should_Succeed()
    {
        var controller = CreateController(new StubRandom(5));

        controller.Get("NUMBER", null, null, null).ShouldBeOfType<ContentResult>();
        controller.Get("String", null, null, null).ShouldBeOfType<ContentResult>();
        controller.Get("UUID", null, null, null).ShouldBeOfType<ContentResult>();
        controller.Get("Color", null, null, null).ShouldBeOfType<ContentResult>();
    }

    [Test]
    public void Get_WithStubRandom_Should_GenerateDeterministicNumber()
    {
        var controller = CreateController(new StubRandom(3));

        var result = controller.Get("number", 0, 10, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.Content.ShouldBe("3");
    }

    private static ToolsRandomController CreateController(Random random) =>
        new(random)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private sealed class StubRandom(int nextValue) : Random
    {
        public override int Next(int minValue, int maxValue) =>
            Math.Clamp(nextValue, minValue, maxValue - 1);

        public override int Next(int maxValue) => Math.Clamp(nextValue, 0, maxValue - 1);
    }
}
