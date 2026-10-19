using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneTests
{
    [Test]
    public void ShouldFormatUtcInstantAsChicagoLocalTime()
    {
        var utc = new DateTime(2024, 6, 15, 14, 15, 0, DateTimeKind.Utc);

        var formatted = ChurchTimeZone.FormatLocal(utc);

        var expected = TimeZoneInfo.ConvertTimeFromUtc(utc, ChurchTimeZone.Chicago)
            .ToString("G", CultureInfo.CurrentCulture);
        formatted.ShouldBe(expected);
    }

    [Test]
    public void ShouldReturnNullWhenDateTimeIsNull()
    {
        ChurchTimeZone.FormatLocal(null).ShouldBeNull();
        ChurchTimeZone.ToLocal(null).ShouldBeNull();
    }

    [Test]
    public void ShouldConvertUtcInstantToChicagoLocalDateTime()
    {
        var utc = new DateTime(2024, 6, 15, 14, 15, 0, DateTimeKind.Utc);

        var local = ChurchTimeZone.ToLocal(utc);

        local.ShouldBe(TimeZoneInfo.ConvertTimeFromUtc(utc, ChurchTimeZone.Chicago));
    }

    [Test]
    public void ShouldTreatUnspecifiedKindAsUtc()
    {
        var unspecified = new DateTime(2024, 6, 15, 14, 15, 0, DateTimeKind.Unspecified);

        var formatted = ChurchTimeZone.FormatLocal(unspecified);

        var expected = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(unspecified, DateTimeKind.Utc),
                ChurchTimeZone.Chicago)
            .ToString("G", CultureInfo.CurrentCulture);
        formatted.ShouldBe(expected);
    }
}
