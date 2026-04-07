using System.Globalization;
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
    public void Get_Should_ReturnBadRequest_WhenValueMissing()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get(null);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void Get_Should_ReturnJsonWithEpochAndIso_WhenValueIsUnixSeconds()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("1700000000");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto.ShouldNotBeNull();
        dto.InputKind.ShouldBe("UnixEpochSeconds");
        dto.UnixSeconds.ShouldBe(1700000000L);
        dto.UnixMilliseconds.ShouldBe(1700000000000L);
        dto.Iso8601Utc.ShouldBe("2023-11-14T22:13:20.0000000Z");
    }

    [Test]
    public void Get_Should_ClassifyMilliseconds_WhenEpochHasManyDigits()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("1700000000000");

        var content = result.ShouldBeOfType<ContentResult>();
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto!.InputKind.ShouldBe("UnixEpochMilliseconds");
        dto.UnixSeconds.ShouldBe(1700000000L);
    }

    [Test]
    public void Get_Should_PreserveOffsetInIso8601WithOffset_WhenIsoHasOffset()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("2024-06-01T12:00:00-05:00");

        var content = result.ShouldBeOfType<ContentResult>();
        var dto = JsonSerializer.Deserialize<TimestampConverterResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions);
        dto!.InputKind.ShouldBe("Iso8601");
        dto.Iso8601WithOffset.ShouldContain("-05:00");
        dto.UnixSeconds.ShouldBe(DateTimeOffset.Parse("2024-06-01T12:00:00-05:00", null, DateTimeStyles.RoundtripKind).ToUnixTimeSeconds());
    }

    [Test]
    public void Get_Should_ReturnBadRequest_WhenValueNotRecognized()
    {
        var controller = new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get("not-a-timestamp");

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
