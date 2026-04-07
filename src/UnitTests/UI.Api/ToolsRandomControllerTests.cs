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
    public void Get_Should_ReturnJsonNumber_When_TypeIsNumber()
    {
        var stubRandom = new StubRandom { NextInt32Result = 424242 };
        var controller = CreateController(stubRandom);

        var result = controller.Get("number");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("number");
        doc.RootElement.GetProperty("value").GetInt32().ShouldBe(424242);
    }

    [Test]
    public void Get_Should_ReturnFixedAlphanumeric_When_TypeIsStringAndRandomReturnsZero()
    {
        var stubRandom = new StubRandom { NextBoundedResult = 0 };
        var controller = CreateController(stubRandom);

        var result = controller.Get("string");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("string");
        doc.RootElement.GetProperty("value").GetString().ShouldBe(new string('A', 24));
    }

    [Test]
    public void Get_Should_ReturnUuidFormat_When_TypeIsUuid()
    {
        var controller = CreateController(new StubRandom());

        var result = controller.Get("uuid");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("uuid");
        var value = doc.RootElement.GetProperty("value").GetString();
        value.ShouldNotBeNull();
        Guid.TryParseExact(value, "D", out _).ShouldBeTrue();
    }

    [Test]
    public void Get_Should_ReturnCssHex_When_TypeIsColor()
    {
        var stubRandom = new StubRandom { NextMinMaxResult = 0xAbCdEf };
        var controller = CreateController(stubRandom);

        var result = controller.Get("color");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("color");
        doc.RootElement.GetProperty("value").GetString().ShouldBe("#abcdef");
    }

    [TestCase("NUMBER")]
    [TestCase("Uuid")]
    public void Get_Should_AcceptTypeCaseInsensitively(string type)
    {
        var stubRandom = new StubRandom { NextInt32Result = 1, NextBoundedResult = 0, NextMinMaxResult = 0 };
        var controller = CreateController(stubRandom);

        var result = controller.Get(type);

        result.ShouldBeOfType<ContentResult>();
        ((ContentResult)result).StatusCode.ShouldBe(200);
    }

    [Test]
    public void Get_Should_Return400_When_TypeMissing()
    {
        var controller = CreateController(new StubRandom());

        var result = controller.Get(null!);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return400_When_TypeUnknown()
    {
        var controller = CreateController(new StubRandom());

        var result = controller.Get("binary");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var stubRandom = new StubRandom { NextInt32Result = 99 };
        var controller = CreateController(stubRandom);
        var first = controller.Get("number");
        var content = first.ShouldBeOfType<ContentResult>();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        var secondContext = new DefaultHttpContext();
        secondContext.Request.Headers.IfNoneMatch = etag;
        var controller2 = new ToolsRandomController(stubRandom)
        {
            ControllerContext = new ControllerContext { HttpContext = secondContext }
        };

        var second = controller2.Get("number");

        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(304);
    }

    private static ToolsRandomController CreateController(Random random)
    {
        return new ToolsRandomController(random)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private sealed class StubRandom : Random
    {
        public int NextInt32Result { get; init; }
        public int NextBoundedResult { get; init; }
        public int NextMinMaxResult { get; init; }

        public override int Next() => NextInt32Result;

        public override int Next(int maxValue) => NextBoundedResult % maxValue;

        public override int Next(int minValue, int maxValue) => NextMinMaxResult;
    }
}
