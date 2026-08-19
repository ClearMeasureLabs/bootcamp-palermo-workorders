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
    private static readonly DateTime FixedUtc = new(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly StubFixedUtcTimeProvider StubTimeProvider = new(FixedUtc);

    [Test]
    public void Get_WithEpochSeconds_Should_ReturnBothFormats()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200", null);

        var payload = AssertJsonOk(result);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.Iso8601Utc.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
        payload.Utc.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.Local.ShouldNotBeNullOrWhiteSpace();
        payload.Relative.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_WithIso8601String_Should_ReturnBothFormats()
    {
        var controller = CreateController();

        var result = controller.Get(null, "2021-01-01T00:00:00Z");

        var payload = AssertJsonOk(result);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.Iso8601Utc.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
        payload.Utc.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.Local.ShouldNotBeNullOrWhiteSpace();
        payload.Relative.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_WithBothParamsProvided_Should_Return400()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200", "2021-01-01T00:00:00Z");

        AssertBadRequest(result, "Provide either epoch or iso, not both.");
    }

    [Test]
    public void Get_WithNeitherParamProvided_Should_Return400()
    {
        var controller = CreateController();

        var result = controller.Get(null, null);

        AssertBadRequest(result, "Provide either epoch or iso query parameter.");
    }

    [Test]
    public void Get_WithInvalidEpochValue_Should_Return400()
    {
        var controller = CreateController();

        var result = controller.Get("not-a-number", null);

        AssertBadRequest(result, "Invalid epoch value.");
    }

    [Test]
    public void Get_WithInvalidIsoString_Should_Return400()
    {
        var controller = CreateController();

        var result = controller.Get(null, "not-a-timestamp");

        AssertBadRequest(result, "Invalid ISO-8601 timestamp.");
    }

    [Test]
    public void Get_WithOutOfRangeEpoch_Should_Return400()
    {
        var controller = CreateController();

        var result = controller.Get(long.MaxValue.ToString(CultureInfo.InvariantCulture), null);

        AssertBadRequest(result, "Epoch value is out of range.");
    }

    [Test]
    public void Get_WithValidEpoch_Should_CalculateRelativeTimeCorrectly()
    {
        var controller = CreateController();

        var result = controller.Get("1609459200", null);

        var payload = AssertJsonOk(result);
        payload.Relative.ShouldBe("5 years ago");
    }

    [Test]
    public void Convert_WithEpochZero_Should_ReturnUnixEpoch()
    {
        var outcome = TimestampConverter.Convert("0", null, StubTimeProvider);

        outcome.Succeeded.ShouldBeTrue();
        outcome.Payload!.EpochSeconds.ShouldBe(0);
        outcome.Payload.Iso8601Utc.ShouldBe("1970-01-01T00:00:00.0000000+00:00");
    }

    private static TimestampConverterController CreateController() =>
        new(StubTimeProvider)
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

    private static void AssertBadRequest(IActionResult result, string expectedError)
    {
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldNotBeNull();
        var json = JsonSerializer.Serialize(badRequest.Value, ConditionalGetEtag.JsonSerializerOptions);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("error").GetString().ShouldBe(expectedError);
    }

    private sealed class StubFixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
