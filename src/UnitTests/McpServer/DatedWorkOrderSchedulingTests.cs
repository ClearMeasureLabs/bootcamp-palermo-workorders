using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

public class DatedWorkOrderSchedulingTests
{
    [Test]
    public void ResolveDueDates_WhenListHasDuplicates_ReturnsEachDateOnceInFirstSeenOrder()
    {
        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System, "2026-11-07,2026-11-07,2026-11-14", 0);

        error.ShouldBeNull();
        dates.ShouldBe([
            new DateOnly(2026, 11, 7),
            new DateOnly(2026, 11, 14)
        ]);
    }

    [Test]
    public void ResolveDueDates_WhenListHasNoDuplicates_IsUnchanged()
    {
        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System, "2026-11-07,2026-11-14", 0);

        error.ShouldBeNull();
        dates.ShouldBe([
            new DateOnly(2026, 11, 7),
            new DateOnly(2026, 11, 14)
        ]);
    }
}
