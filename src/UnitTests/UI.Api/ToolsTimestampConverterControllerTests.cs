using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsTimestampConverterControllerTests
{
    private const long KnownEpochSeconds = 1_700_000_000L;
    private static readonly DateTimeOffset KnownInstant =
        DateTimeOffset.FromUnixTimeSeconds(KnownEpochSeconds);

    [Test]
    public void Get_Should_ReturnJsonConversions_When_EpochSeconds()
    {
        var result = CreateController().Get(KnownEpochSeconds.ToString(), null);

        var payload = AssertOkPayload(result);
        AssertMatchesKnownInstant(payload);
    }

    [Test]
    public void Get_Should_ReturnJsonConversions_When_EpochMilliseconds()
    {
        var ms = KnownInstant.ToUnixTimeMilliseconds();
        ms.ShouldBeGreaterThan(ToolsTimestampConverterController.MillisecondsThreshold);

        var result = CreateController().Get(ms.ToString(), null);

        var payload = AssertOkPayload(result);
        AssertMatchesKnownInstant(payload);
    }

    [Test]
    public void Get_Should_ReturnJsonConversions_When_Iso8601()
    {
        var iso = KnownInstant.UtcDateTime.ToString("O");

        var result = CreateController().Get(null, iso);

        var payload = AssertOkPayload(result);
        AssertMatchesKnownInstant(payload);
        payload.Iso8601.ShouldBe(KnownInstant.ToUniversalTime().ToString("O"));
    }

    [Test]
    public void Get_Should_Return400_When_QueryMissing()
    {
        var result = CreateController().Get(null, null);

        AssertProblem400(result);
    }

    [Test]
    public void Get_Should_Return400_When_BothEpochAndIso()
    {
        var result = CreateController().Get(KnownEpochSeconds.ToString(), KnownInstant.ToString("O"));

        AssertProblem400(result);
    }

    [Test]
    public void Get_Should_Return400_When_EpochInvalid()
    {
        var result = CreateController().Get("not-a-number", null);

        AssertProblem400(result);
    }

    [Test]
    public void Get_Should_Return400_When_IsoInvalid()
    {
        var result = CreateController().Get(null, "not-an-iso-timestamp");

        AssertProblem400(result);
    }

    private static TimestampConverterResponse AssertOkPayload(IActionResult result)
    {
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        return ok.Value.ShouldBeOfType<TimestampConverterResponse>();
    }

    private static void AssertMatchesKnownInstant(TimestampConverterResponse payload)
    {
        var expected = ToolsTimestampConverterController.BuildResponse(KnownInstant);
        payload.EpochSeconds.ShouldBe(expected.EpochSeconds);
        payload.EpochMilliseconds.ShouldBe(expected.EpochMilliseconds);
        payload.Iso8601.ShouldBe(expected.Iso8601);
        payload.Rfc1123.ShouldBe(expected.Rfc1123);
        payload.UnixUtcDisplay.ShouldBe(expected.UnixUtcDisplay);
    }

    private static void AssertProblem400(IActionResult result)
    {
        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
        objectResult.Value.ShouldBeOfType<ProblemDetails>();
    }

    private static ToolsTimestampConverterController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
