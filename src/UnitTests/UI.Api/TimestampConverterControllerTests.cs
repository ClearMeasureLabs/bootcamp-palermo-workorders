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
    private static readonly DateTimeOffset FixedInstant =
        new(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private TimestampConverterController CreateController()
    {
        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Test]
    public void Get_EpochSeconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200", null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = Deserialize(content.Content!);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe(FixedInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.UtcFormatted.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.LocalFormatted.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_EpochMilliseconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200000", null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = Deserialize(content.Content!);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe(FixedInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.UtcFormatted.ShouldBe("2021-01-01 00:00:00 UTC");
    }

    [Test]
    public void Get_Iso8601_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get(null, "2021-01-01T00:00:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        var payload = Deserialize(content.Content!);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe(FixedInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.UtcFormatted.ShouldBe("2021-01-01 00:00:00 UTC");
    }

    [Test]
    public void Get_MissingBothParams_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get(null, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_BothParamsSupplied_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200", "2021-01-01T00:00:00Z");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("both");
    }

    [Test]
    public void Get_InvalidEpochFormat_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get("abc", null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("abc");
    }

    [Test]
    public void Get_InvalidIsoFormat_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get(null, "not-a-date");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(400);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldNotBeNull();
        details.Detail!.ShouldContain("not-a-date");
    }

    private static TimestampConverterResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<TimestampConverterResponse>(json, ConditionalGetEtag.JsonSerializerOptions)!
            ?? throw new InvalidOperationException("Expected JSON payload.");
}
