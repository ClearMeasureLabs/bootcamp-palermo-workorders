using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Today_WhenLateEveningChicago_ReturnsChicagoDateNotUtcDate()
    {
        // 2026-08-29 23:00 CDT (UTC-5) == 2026-08-30 04:00 UTC
        var clock = ChicagoLocalOn(2026, 8, 29, 23, 0, 0);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void Today_WhenBeforeSpringForward_ReturnsChicagoDate()
    {
        // DST starts 2026-03-08 02:00 → 03:00; evening before is still CST
        var clock = ChicagoLocalOn(2026, 3, 7, 23, 0, 0);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 3, 7));
    }

    [Test]
    public void Today_WhenAfterSpringForward_ReturnsChicagoDate()
    {
        var clock = ChicagoLocalOn(2026, 3, 8, 12, 0, 0);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 3, 8));
    }

    [Test]
    public void Today_WhenBeforeFallBack_ReturnsChicagoDate()
    {
        // DST ends 2026-11-01 02:00 → 01:00; evening before is still CDT
        var clock = ChicagoLocalOn(2026, 10, 31, 23, 0, 0);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 10, 31));
    }

    [Test]
    public void Today_WhenAfterFallBack_ReturnsChicagoDate()
    {
        var clock = ChicagoLocalOn(2026, 11, 1, 12, 0, 0);

        ChurchTimeZone.Today(clock).ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Test]
    public void ComingSaturday_WhenTodayIsSaturday_ReturnsToday()
    {
        // 2026-08-29 is a Saturday
        var clock = ChicagoLocalOn(2026, 8, 29, 12, 0, 0);

        ChurchTimeZone.ComingSaturday(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void ComingSaturday_WhenTodayIsFriday_ReturnsNextDay()
    {
        // 2026-08-28 is a Friday
        var clock = ChicagoLocalOn(2026, 8, 28, 12, 0, 0);

        ChurchTimeZone.ComingSaturday(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void ComingSaturday_WhenTodayIsSunday_ReturnsSixDaysAhead()
    {
        // 2026-08-30 is a Sunday
        var clock = ChicagoLocalOn(2026, 8, 30, 12, 0, 0);

        ChurchTimeZone.ComingSaturday(clock).ShouldBe(new DateOnly(2026, 9, 5));
    }

    [Test]
    public void Today_WhenProviderIsNull_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ChurchTimeZone.Today(null!));
    }

    private static FixedTimeProvider ChicagoLocalOn(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        var local = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, ChurchTimeZone.Chicago);
        return new FixedTimeProvider(new DateTimeOffset(utc, TimeSpan.Zero));
    }
}
