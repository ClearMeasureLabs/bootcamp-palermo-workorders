using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class ChurchTimeZoneTests
{
    [Test]
    public void ComingSunday_WhenTodayIsSunday_ReturnsToday()
    {
        var timeProvider = new StubTimeProvider(new DateTimeOffset(2024, 1, 7, 18, 0, 0, TimeSpan.Zero));

        var result = ChurchTimeZone.ComingSunday(timeProvider);

        result.ShouldBe(new DateOnly(2024, 1, 7));
    }

    [Test]
    public void ComingSunday_WhenTodayIsSaturday_ReturnsNextSunday()
    {
        var timeProvider = new StubTimeProvider(new DateTimeOffset(2024, 1, 6, 18, 0, 0, TimeSpan.Zero));

        var result = ChurchTimeZone.ComingSunday(timeProvider);

        result.ShouldBe(new DateOnly(2024, 1, 7));
    }

    [Test]
    public void ComingSunday_WhenTodayIsMonday_ReturnsNextSunday()
    {
        var timeProvider = new StubTimeProvider(new DateTimeOffset(2024, 1, 8, 18, 0, 0, TimeSpan.Zero));

        var result = ChurchTimeZone.ComingSunday(timeProvider);

        result.ShouldBe(new DateOnly(2024, 1, 14));
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
