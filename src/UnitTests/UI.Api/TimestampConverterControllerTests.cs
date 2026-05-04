using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
    [Test]
    public void Get_Should_Return400_When_ValueMissing()
    {
        var controller = CreateController();

        var result = controller.Get(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_Return400_When_ValueWhitespace()
    {
        var controller = CreateController();

        var result = controller.Get("   ");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnJson_When_EpochSeconds()
    {
        var controller = CreateController();

        var result = controller.Get("0");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("application/json");
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto.ShouldNotBeNull();
        dto.UnixTimeSeconds.ShouldBe(0L);
        dto.UnixTimeMilliseconds.ShouldBe(0L);
        dto.UtcDate.ShouldBe("1970-01-01");
        dto.Iso8601Utc.ShouldStartWith("1970-01-01T00:00:00");
    }

    [Test]
    public void Get_Should_UseMilliseconds_When_AbsoluteEpochAtLeast1e11()
    {
        var controller = CreateController();

        var result = controller.Get("100000000000");

        var content = result.ShouldBeOfType<ContentResult>();
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto.ShouldNotBeNull();
        dto.UnixTimeSeconds.ShouldBe(100_000_000L);
        dto.UnixTimeMilliseconds.ShouldBe(100_000_000_000L);
    }

    [Test]
    public void Get_Should_ParseIso8601()
    {
        var controller = CreateController();

        var result = controller.Get("2024-06-15T14:30:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto.ShouldNotBeNull();
        dto.UnixTimeSeconds.ShouldBe(new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesWeakEtag()
    {
        var controller1 = CreateController();
        var first = controller1.Get("0");
        first.ShouldBeOfType<ContentResult>();
        var etag = controller1.Response.Headers.ETag.ToString();

        var httpContext2 = new DefaultHttpContext();
        httpContext2.Request.Headers.IfNoneMatch = etag;
        var controller2 = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext2 }
        };

        var second = controller2.Get("0");

        second.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(304);
    }

    private static TimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
