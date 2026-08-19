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
    public void Get_EpochSeconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();
        var result = controller.Get("1609459200", null);

        var payload = AssertJsonOk(result);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
        payload.UtcFormatted.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.LocalFormatted.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_EpochMilliseconds_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();
        var result = controller.Get("1609459200000", null);

        var payload = AssertJsonOk(result);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
    }

    [Test]
    public void Get_Iso8601_Should_ReturnBothFormatsAndFormatted()
    {
        var controller = CreateController();
        var result = controller.Get(null, "2021-01-01T00:00:00Z");

        var payload = AssertJsonOk(result);
        payload.EpochSeconds.ShouldBe(1609459200L);
        payload.EpochMilliseconds.ShouldBe(1609459200000L);
        payload.Iso8601.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
    }

    [Test]
    public void Get_MissingBothParams_Should_Return400ProblemDetails()
    {
        var controller = CreateController();
        var result = controller.Get(null, null);

        AssertProblem400(result, "Provide exactly one of 'epoch' or 'iso' query parameters.");
    }

    [Test]
    public void Get_BothParamsSupplied_Should_Return400ProblemDetails()
    {
        var controller = CreateController();
        var result = controller.Get("1609459200", "2021-01-01T00:00:00Z");

        AssertProblem400(result, "Provide only one of 'epoch' or 'iso', not both.");
    }

    [Test]
    public void Get_InvalidEpochFormat_Should_Return400ProblemDetails()
    {
        var controller = CreateController();
        var result = controller.Get("abc", null);

        AssertProblem400(result, "Unable to parse epoch value: 'abc'.");
    }

    [Test]
    public void Get_InvalidIsoFormat_Should_Return400ProblemDetails()
    {
        var controller = CreateController();
        var result = controller.Get(null, "not-a-date");

        AssertProblem400(result, "Unable to parse ISO-8601 value: 'not-a-date'.");
    }

    private static TimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

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

    private static void AssertProblem400(IActionResult result, string expectedDetail)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe(expectedDetail);
    }
}
