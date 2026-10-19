using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WeeklyCompletedSummaryQueryHandlerTests
{
    [Test]
    public void Should_CountCompletedSince_IncludeOnlyDatesInWindow()
    {
        var since = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
        DateTime?[] dates =
        [
            since.AddDays(1),
            since.AddDays(-1),
            null,
            since
        ];

        var count = WeeklyCompletedSummaryQueryHandler.CountCompletedSince(dates, since);

        count.ShouldBe(2);
    }
}
