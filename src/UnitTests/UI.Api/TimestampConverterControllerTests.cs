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
    private static readonly DateTimeOffset KnownInstant =
        new(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private TimestampConverterController CreateController()
    {
        return new TimestampConverterController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static TimestampConverterResponse Deserialize(ContentResult content)
    {
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }

    [Test]
    public void Get_EpochSeconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get(1609459200, null);

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = Deserialize(content);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.EpochMilliseconds.ShouldBe(1609459200000);
        payload.Iso8601.ShouldBe(KnownInstant.ToString("O", CultureInfo.InvariantCulture));
        payload.UtcFormatted.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.LocalFormatted.ShouldBe(
            TimeZoneInfo.ConvertTime(KnownInstant, TimeZoneInfo.Local)
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
    }

    [Test]
    public void Get_EpochMilliseconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get(1609459200000, null);

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.EpochMilliseconds.ShouldBe(1609459200000);
        payload.Iso8601.ShouldBe(KnownInstant.ToString("O", CultureInfo.InvariantCulture));
    }

    [Test]
    public void Get_Iso8601_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();

        var result = controller.Get(null, "2021-01-01T00:00:00Z");

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.EpochMilliseconds.ShouldBe(1609459200000);
        payload.Iso8601.ShouldBe(KnownInstant.ToString("O", CultureInfo.InvariantCulture));
    }

    [Test]
    public void Get_MissingBothParams_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get(null, null);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldBe("Exactly one of 'epoch' or 'iso' query parameter is required.");
    }

    [Test]
    public void Get_BothParamsSupplied_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get(1234, "2021-01-01T00:00:00Z");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldBe("Supply only one of 'epoch' or 'iso', not both.");
    }

    [Test]
    public void Get_InvalidIsoFormat_Should_Return400ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Get(null, "not-a-date");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldBe("Unable to parse ISO-8601 value 'not-a-date'.");
    }
}
