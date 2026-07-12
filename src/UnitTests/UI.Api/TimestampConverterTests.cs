using System.Globalization;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterTests
{
    private static readonly DateTimeOffset FixedInstant =
        new(2024, 3, 30, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void Should_ReturnSuccess_When_ValidEpochSecondsProvided()
    {
        var result = TimestampConverter.TryConvertFromEpoch("1711792800");

        result.Success.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.InputKind.ShouldBe("epoch");
        result.Payload.EpochSeconds.ShouldBe(FixedInstant.ToUnixTimeSeconds());
        result.Payload.EpochMilliseconds.ShouldBe(FixedInstant.ToUnixTimeMilliseconds());
        result.Payload.Iso8601Utc.ShouldBe(FixedInstant.ToString("O", CultureInfo.InvariantCulture));
        result.Payload.Utc.ShouldBe("2024-03-30 10:00:00 UTC");
    }

    [Test]
    public void Should_ReturnSuccess_When_ValidEpochMillisecondsProvided()
    {
        var result = TimestampConverter.TryConvertFromEpoch("1711792800000");

        result.Success.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.EpochSeconds.ShouldBe(FixedInstant.ToUnixTimeSeconds());
        result.Payload.EpochMilliseconds.ShouldBe(FixedInstant.ToUnixTimeMilliseconds());
        result.Payload.InputKind.ShouldBe("epoch");
    }

    [Test]
    public void Should_ReturnFailure_When_EpochIsNonNumeric()
    {
        TimestampConverter.TryConvertFromEpoch("abc").Success.ShouldBeFalse();
        TimestampConverter.TryConvertFromEpoch("12.5").Success.ShouldBeFalse();
        TimestampConverter.TryConvertFromEpoch("   ").Success.ShouldBeFalse();
    }

    [Test]
    public void Should_ReturnFailure_When_EpochOutOfRange()
    {
        var result = TimestampConverter.TryConvertFromEpoch("9223372036854775807");

        result.Success.ShouldBeFalse();
        result.ErrorDetail.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_ReturnSuccess_When_ValidIso8601WithZSuffix()
    {
        var result = TimestampConverter.TryConvertFromIso("2026-07-12T15:00:00Z");

        result.Success.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.InputKind.ShouldBe("iso");
        result.Payload.Iso8601Utc.ShouldBe("2026-07-12T15:00:00.0000000+00:00");
        result.Payload.EpochSeconds.ShouldBe(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    [Test]
    public void Should_ReturnSuccess_When_ValidIso8601WithOffset()
    {
        var result = TimestampConverter.TryConvertFromIso("2026-07-12T15:00:00+05:30");

        result.Success.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.Iso8601Utc.ShouldBe("2026-07-12T09:30:00.0000000+00:00");
    }

    [Test]
    public void Should_ReturnFailure_When_IsoIsMalformed()
    {
        var result = TimestampConverter.TryConvertFromIso("not-a-date");

        result.Success.ShouldBeFalse();
        result.ErrorDetail.ShouldNotBeNullOrWhiteSpace();
        result.ErrorDetail!.ShouldContain("ISO-8601");
    }

    [Test]
    public void Should_ReturnFailure_When_IsoIsEmpty()
    {
        TimestampConverter.TryConvertFromIso(null).Success.ShouldBeFalse();
        TimestampConverter.TryConvertFromIso("").Success.ShouldBeFalse();
        TimestampConverter.TryConvertFromIso("   ").Success.ShouldBeFalse();
    }

    [Test]
    public void Should_NormalizeToUtc_When_IsoHasLocalOffset()
    {
        var result = TimestampConverter.TryConvertFromIso("2026-07-12T15:00:00+05:30");

        result.Success.ShouldBeTrue();
        result.Payload!.Iso8601Utc.ShouldEndWith("+00:00");
        result.Payload.EpochSeconds.ShouldBe(
            new DateTimeOffset(2026, 7, 12, 9, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    [Test]
    public void Should_IncludeLocalTimeZoneId_When_ConversionSucceeds()
    {
        var result = TimestampConverter.TryConvertFromEpoch("1711792800");

        result.Success.ShouldBeTrue();
        result.Payload!.LocalTimeZoneId.ShouldBe(TimeZoneInfo.Local.Id);
        result.Payload.Local.ShouldNotBeNullOrWhiteSpace();
    }
}
