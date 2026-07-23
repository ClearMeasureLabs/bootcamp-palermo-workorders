using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Globalization;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class TimeControllerTests
{
    [Test]
    public void Get_ReturnsOkResult()
    {
        var controller = new TimeController();

        var result = controller.Get();

        result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Get_ReturnsJsonWithUtcProperty()
    {
        var controller = new TimeController();
        var beforeRequest = DateTime.UtcNow;

        var result = controller.Get() as OkObjectResult;

        result.ShouldNotBeNull();
        result.Value.ShouldNotBeNull();
        
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("utc", out var utcProperty).ShouldBeTrue();
        
        var utcString = utcProperty.GetString();
        utcString.ShouldNotBeNullOrEmpty();
        
        // Verify it's valid ISO-8601 format and parse with RoundtripKind to preserve UTC
        var parsedTime = DateTime.Parse(utcString!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        parsedTime.Kind.ShouldBe(DateTimeKind.Utc);
        
        // Verify the time is reasonable (within a few seconds of now)
        var afterRequest = DateTime.UtcNow;
        parsedTime.ShouldBeGreaterThanOrEqualTo(beforeRequest.AddSeconds(-5));
        parsedTime.ShouldBeLessThanOrEqualTo(afterRequest.AddSeconds(5));
    }
}
