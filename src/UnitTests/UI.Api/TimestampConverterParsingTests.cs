using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterParsingTests
{
    [Test]
    public void ParseUnixEpochNumeric_Should_UseSeconds_When_BelowMsThreshold()
    {
        var instant = TimestampConverterParsing.ParseUnixEpochNumeric(0);
        instant.ToUnixTimeSeconds().ShouldBe(0);
    }

    [Test]
    public void ParseUnixEpochNumeric_Should_UseMilliseconds_When_AtOrAboveMsThreshold()
    {
        var ms = 1774872000000L;
        var instant = TimestampConverterParsing.ParseUnixEpochNumeric(ms);
        instant.ToUnixTimeMilliseconds().ShouldBe(ms);
    }

    [Test]
    public void TryParseIso8601_Should_ParseRoundTripZ()
    {
        var ok = TimestampConverterParsing.TryParseIso8601("2026-03-30T12:00:00Z", out var instant);
        ok.ShouldBeTrue();
        instant.ToUnixTimeSeconds().ShouldBe(1774872000);
    }
}
