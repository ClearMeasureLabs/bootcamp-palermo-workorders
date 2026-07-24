using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class PingControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonWithPongAndTimestamp_When_Called()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var stubTimeProvider = new StubFixedUtcTimeProvider(fixedUtc);
        var controller = new PingController(stubTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<PingResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Pong.ShouldBe("pong");
        payload.Timestamp.ShouldBe("2026-03-30T12:00:00.0000000Z");
    }

    [Test]
    public void Get_Should_FormatTimestampAsIso8601Utc_When_Called()
    {
        var fixedUtc = new DateTime(2026, 7, 25, 4, 36, 57, DateTimeKind.Utc);
        var stubTimeProvider = new StubFixedUtcTimeProvider(fixedUtc);
        var controller = new PingController(stubTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<PingResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        var expected = new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture);
        payload!.Timestamp.ShouldBe(expected);
        DateTimeOffset.TryParse(payload.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _).ShouldBeTrue();
    }

    private sealed class StubFixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
