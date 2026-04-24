using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterParseTests
{
    [Test]
    public void TryParseInstant_Should_ParseNegativeEpochSeconds()
    {
        var ok = TimestampConverterController.TryParseInstant("-1", out var instant, out var error);
        ok.ShouldBeTrue();
        error.ShouldBeEmpty();
        instant.ShouldBe(DateTimeOffset.FromUnixTimeSeconds(-1));
    }

    [Test]
    public void TryParseInstant_Should_Fail_When_NotIntegerAndNotIso()
    {
        var ok = TimestampConverterController.TryParseInstant("not-a-date", out _, out var error);
        ok.ShouldBeFalse();
        error.ShouldContain("ISO-8601");
    }

    [Test]
    public void TryParseInstant_Should_ParseFractionalAsIso_NotEpoch()
    {
        var ok = TimestampConverterController.TryParseInstant("2020-01-01T00:00:00.500Z", out var instant, out _);
        ok.ShouldBeTrue();
        instant.UtcTicks.ShouldBe(new DateTimeOffset(2020, 1, 1, 0, 0, 0, 500, TimeSpan.Zero).UtcTicks);
    }
}
