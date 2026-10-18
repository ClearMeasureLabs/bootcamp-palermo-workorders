using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimeControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonUtcAndDayOfWeek_ForFixedClock()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var stubTimeProvider = new StubFixedUtcTimeProvider(fixedUtc);
        var controller = new TimeController(stubTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimeResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        var expectedUtc = new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture);
        payload.Utc.ShouldBe(expectedUtc);
        payload.DayOfWeek.ShouldBe("Monday");
    }

    private sealed class StubFixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
