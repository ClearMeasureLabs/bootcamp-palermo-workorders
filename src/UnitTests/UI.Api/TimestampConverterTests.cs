using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterTests
{
    [Test]
    public void Parse_Should_ConvertUnixEpochSeconds_ToCanonical()
    {
        var instant = TimestampConverter.ParseEpoch("1704067200");

        instant.ToUnixTimeSeconds().ShouldBe(1704067200L);
        instant.ToUniversalTime().Year.ShouldBe(2024);
        instant.ToUniversalTime().Month.ShouldBe(1);
        instant.ToUniversalTime().Day.ShouldBe(1);
    }

    [Test]
    public void Parse_Should_ConvertUnixEpochMilliseconds_ToCanonical()
    {
        var instant = TimestampConverter.ParseEpoch("1704067200000");

        instant.ToUnixTimeSeconds().ShouldBe(1704067200L);
        instant.ToUnixTimeMilliseconds().ShouldBe(1704067200000L);
    }

    [Test]
    public void Parse_Should_ConvertIso8601_ToCanonical()
    {
        var instant = TimestampConverter.ParseIso("2024-01-01T00:00:00Z");

        instant.ToUnixTimeSeconds().ShouldBe(1704067200L);
        instant.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void Parse_Should_ThrowOnInvalidEpoch_When_NotANumber()
    {
        Should.Throw<FormatException>(() => TimestampConverter.ParseEpoch("not-a-number"))
            .Message.ShouldContain("integer");
    }

    [Test]
    public void Parse_Should_ThrowOnInvalidIso_When_MalformedDate()
    {
        Should.Throw<FormatException>(() => TimestampConverter.ParseIso("invalid-date"))
            .Message.ShouldContain("ISO-8601");
    }
}
