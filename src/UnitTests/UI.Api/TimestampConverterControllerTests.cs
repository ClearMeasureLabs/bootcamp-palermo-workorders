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
    private static TimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Test]
    public void Get_Should_ReturnOk_WithAllFields_When_EpochSupplied()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "1704067200", iso: null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UnixSeconds.ShouldBe(1704067200);
        payload.UnixMilliseconds.ShouldBe(1704067200000);
        payload.Iso8601Utc.ShouldBe(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture));
        payload.UtcDisplay.ShouldBe("Monday, 01 January 2024 00:00:00 UTC");
        payload.Rfc1123.ShouldBe("Mon, 01 Jan 2024 00:00:00 GMT");
    }

    [Test]
    public void Get_Should_ReturnOk_WithAllFields_When_IsoSupplied()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: "2024-01-01T00:00:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UnixSeconds.ShouldBe(1704067200);
        payload.UnixMilliseconds.ShouldBe(1704067200000);
        payload.Iso8601Utc.ShouldBe(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture));
        payload.UtcDisplay.ShouldBe("Monday, 01 January 2024 00:00:00 UTC");
        payload.Rfc1123.ShouldBe("Mon, 01 Jan 2024 00:00:00 GMT");
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_NeitherParameterSupplied()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_BothParametersSupplied()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "1704067200", iso: "2024-01-01T00:00:00Z");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_EpochUnparseable()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "not-a-number", iso: null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_IsoUnparseable()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: null, iso: "not-a-date");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public void Get_Should_ReturnBadRequest_When_EpochOutOfRange()
    {
        var controller = CreateController();

        var result = controller.Get(epoch: "999999999999999", iso: null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }
}
