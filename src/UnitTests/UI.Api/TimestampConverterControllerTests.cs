using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterControllerTests
{
    [Test]
    public void Get_Should_Return200Json_With_EpochSecondsAndIso8601_When_ValidEpochProvided()
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
        payload!.EpochSeconds.ShouldBe(1704067200L);
        payload.EpochMilliseconds.ShouldBe(1704067200000L);
        payload.Iso8601Utc.ShouldBe("2024-01-01T00:00:00.0000000Z");
        payload.UtcDisplay.ShouldBe("2024-01-01 00:00:00 UTC");
        payload.LocalDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_BothEpochAndIsoProvided()
    {
        var controller = CreateController();
        var result = controller.Get(epoch: "1704067200", iso: "2024-01-01T00:00:00Z");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("exactly one");
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_NeitherEpochNorIsoProvided()
    {
        var controller = CreateController();
        var result = controller.Get(epoch: null, iso: null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("required");
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_EpochIsInvalid()
    {
        var controller = CreateController();
        var result = controller.Get(epoch: "not-a-number", iso: null);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("integer");
    }

    [Test]
    public void Get_Should_Return400ProblemDetails_When_IsoIsInvalid()
    {
        var controller = CreateController();
        var result = controller.Get(epoch: null, iso: "invalid-date");

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldNotBeNull();
        problem.Detail!.ShouldContain("ISO-8601");
    }

    [Test]
    public void Get_Should_RespectLocalTimeZone_In_LocalDisplay()
    {
        var utcZone = TimeZoneInfo.Utc;
        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

        var utcController = CreateController(utcZone);
        var easternController = CreateController(eastern);

        var utcResult = utcController.Get(epoch: "1704067200", iso: null).ShouldBeOfType<ContentResult>();
        var easternResult = easternController.Get(epoch: "1704067200", iso: null).ShouldBeOfType<ContentResult>();

        var utcPayload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            utcResult.Content!,
            ConditionalGetEtag.JsonSerializerOptions)!;
        var easternPayload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            easternResult.Content!,
            ConditionalGetEtag.JsonSerializerOptions)!;

        utcPayload.LocalDisplay.ShouldBe("2024-01-01 00:00:00 +00:00");
        easternPayload.LocalDisplay.ShouldContain("2023-12-31");
    }

    private static TimestampConverterController CreateController(TimeZoneInfo? localTimeZone = null)
    {
        return new TimestampConverterController(NullLogger<TimestampConverterController>.Instance, localTimeZone)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
