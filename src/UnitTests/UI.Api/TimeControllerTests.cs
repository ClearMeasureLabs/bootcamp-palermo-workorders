using System.Globalization;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimeControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextIso8601Utc_ForFixedClock()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var controller = CreateController(fixedUtc);

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        var expected = new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture);
        content.Content.ShouldBe(expected);
    }

    [Test]
    public void Get_Should_ReturnUnixSeconds_When_FormatUnix()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var controller = CreateController(fixedUtc);

        var result = controller.Get(format: "unix");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBe(new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture));
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_FormatUnknown()
    {
        var controller = CreateController(new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc));

        var result = controller.Get(format: "bogus");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static TimeController CreateController(DateTime fixedUtc)
    {
        return new TimeController(new StubFixedUtcTimeProvider(fixedUtc))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private sealed class StubFixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
