using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimestampConverterTests
{
    [Test]
    public void Should_ParseEpochSeconds_When_ValidSecondsProvided()
    {
        var success = TimestampConverter.TryFromEpoch("1718208000", out var instant, out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.ToUnixTimeSeconds().ShouldBe(1718208000L);
    }

    [Test]
    public void Should_ParseEpochMilliseconds_When_ValueExceedsSecondsRange()
    {
        var success = TimestampConverter.TryFromEpoch("1718208000000", out var instant, out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.ToUnixTimeSeconds().ShouldBe(1718208000L);
        instant.ToUnixTimeMilliseconds().ShouldBe(1718208000000L);
    }

    [Test]
    public void Should_ParseEpochSeconds_When_ValueAtSecondsUpperBound()
    {
        var success = TimestampConverter.TryFromEpoch("9999999999", out var instant, out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.ToUnixTimeSeconds().ShouldBe(9999999999L);
    }

    [Test]
    public void Should_ParseEpochMilliseconds_When_ValueAtMillisecondsLowerBound()
    {
        var success = TimestampConverter.TryFromEpoch("10000000000", out var instant, out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.ToUnixTimeMilliseconds().ShouldBe(10000000000L);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Should_ReturnError_When_EpochIsNullOrWhitespace(string? epoch)
    {
        var success = TimestampConverter.TryFromEpoch(epoch, out _, out var error);

        success.ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_ReturnError_When_EpochIsNonNumeric()
    {
        var success = TimestampConverter.TryFromEpoch("abc", out _, out var error);

        success.ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_ReturnError_When_EpochIsOutOfRange()
    {
        var success = TimestampConverter.TryFromEpoch("9999999999999999999", out _, out var error);

        success.ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_ParseIso8601_When_RoundTripFormatProvided()
    {
        var success = TimestampConverter.TryFromIso8601(
            "2024-06-12T16:00:00.0000000+00:00",
            out var instant,
            out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.ToUnixTimeSeconds().ShouldBe(1718208000L);
    }

    [Test]
    public void Should_ParseIso8601_When_UtcZSuffixProvided()
    {
        var success = TimestampConverter.TryFromIso8601("2024-06-12T16:00:00Z", out var instant, out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.Offset.ShouldBe(TimeSpan.Zero);
        instant.ToUnixTimeSeconds().ShouldBe(1718208000L);
    }

    [Test]
    public void Should_ParseIso8601_When_NumericOffsetProvided()
    {
        var success = TimestampConverter.TryFromIso8601(
            "2026-07-12T15:30:00+05:30",
            out var instant,
            out var error);

        success.ShouldBeTrue();
        error.ShouldBeNull();
        instant.Offset.ShouldBe(TimeSpan.FromHours(5.5));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Should_ReturnError_When_IsoIsNullOrWhitespace(string? iso)
    {
        var success = TimestampConverter.TryFromIso8601(iso, out _, out var error);

        success.ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_ReturnError_When_IsoIsInvalid()
    {
        var success = TimestampConverter.TryFromIso8601("not-a-date", out _, out var error);

        success.ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_BuildResponse_WithBothEpochAndIso_When_FromEpochInput()
    {
        TimestampConverter.TryFromEpoch("1718208000", out var instant, out _).ShouldBeTrue();

        var response = TimestampConverter.ToResponse(instant, "epoch");

        response.InputKind.ShouldBe("epoch");
        response.EpochSeconds.ShouldBe(1718208000L);
        response.EpochMilliseconds.ShouldBe(1718208000000L);
        response.Iso8601.ShouldBe(instant.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        response.UtcDisplay.ShouldNotBeNullOrWhiteSpace();
        response.LocalDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Should_BuildResponse_WithBothEpochAndIso_When_FromIsoInput()
    {
        TimestampConverter.TryFromIso8601("2024-06-12T16:00:00Z", out var instant, out _).ShouldBeTrue();

        var response = TimestampConverter.ToResponse(instant, "iso");

        response.InputKind.ShouldBe("iso");
        response.EpochSeconds.ShouldBe(1718208000L);
        response.EpochMilliseconds.ShouldBe(1718208000000L);
        response.Iso8601.ShouldNotBeNullOrWhiteSpace();
        response.UtcDisplay.ShouldNotBeNullOrWhiteSpace();
        response.LocalDisplay.ShouldNotBeNullOrWhiteSpace();
    }
}
