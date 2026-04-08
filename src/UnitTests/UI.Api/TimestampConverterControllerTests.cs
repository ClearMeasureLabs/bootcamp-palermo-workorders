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
    public void Get_Should_Return400_When_TimestampMissing()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnEpochSeconds_When_SecondsInput()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("0");

        var payload = AssertJsonOk(result);
        payload.InputKind.ShouldBe("epoch_seconds");
        payload.UnixSeconds.ShouldBe(0L);
        payload.UnixMilliseconds.ShouldBe(0L);
        payload.Iso8601Utc.ShouldBe("1970-01-01T00:00:00.0000000Z");
    }

    [Test]
    public void Get_Should_ReturnEpochMilliseconds_When_MillisecondsInput()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("1700000000000");

        var payload = AssertJsonOk(result);
        payload.InputKind.ShouldBe("epoch_milliseconds");
        payload.UnixMilliseconds.ShouldBe(1700000000000L);
        payload.UnixSeconds.ShouldBe(1700000000L);
    }

    [Test]
    public void Get_Should_ReturnIso8601_When_IsoInput()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("2024-01-15T12:30:45Z");

        var payload = AssertJsonOk(result);
        payload.InputKind.ShouldBe("iso8601");
        payload.Iso8601Utc.ShouldBe("2024-01-15T12:30:45.0000000Z");
        payload.UnixSeconds.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var httpContext = new DefaultHttpContext();
        var controller = new TimestampConverterController { ControllerContext = new ControllerContext { HttpContext = httpContext } };

        var first = controller.Get("1000");
        var etag = httpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag;
        controller = new TimestampConverterController { ControllerContext = new ControllerContext { HttpContext = httpContext } };

        var second = controller.Get("1000");

        var status = second.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(304);
    }

    private static TimestampConverterResponse AssertJsonOk(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
