using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class DueDateUrgencyCalculatorTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Calculate_WhenDueDateNull_ReturnsNone()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
        var clock = ChicagoNoonOn(2026, 8, 29);

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
        DueDateUrgencyCalculator.CssClass(DueDateUrgency.None).ShouldBe(string.Empty);
        DueDateUrgencyCalculator.ScreenReaderText(DueDateUrgency.None).ShouldBeNull();
    }

    [Test]
    public void Calculate_WhenOpenAndDueToday_ReturnsDueToday()
    {
        var today = ChurchTimeZone.Today(ChicagoNoonOn(2026, 8, 29));
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.Assigned,
            DueDate = today
        };

        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, ChicagoNoonOn(2026, 8, 29));

        urgency.ShouldBe(DueDateUrgency.DueToday);
        DueDateUrgencyCalculator.CssClass(urgency).ShouldBe("due-date-today");
        DueDateUrgencyCalculator.ScreenReaderText(urgency).ShouldBe("Due today");
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("InProgress")]
    public void Calculate_WhenOpenAndPastDue_ReturnsOverdue(string statusKey)
    {
        var clock = ChicagoNoonOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.FromKey(statusKey),
            DueDate = ChurchTimeZone.Today(clock).AddDays(-1)
        };

        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, clock);

        urgency.ShouldBe(DueDateUrgency.Overdue);
        DueDateUrgencyCalculator.CssClass(urgency).ShouldBe("due-date-overdue");
        DueDateUrgencyCalculator.ScreenReaderText(urgency).ShouldBe("Overdue");
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Calculate_WhenTerminalStatus_ReturnsNoneEvenIfPastDue(string statusKey)
    {
        var clock = ChicagoNoonOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.FromKey(statusKey),
            DueDate = ChurchTimeZone.Today(clock).AddDays(-3)
        };

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
    }

    [Test]
    public void Calculate_WhenOpenAndFutureDue_ReturnsNone()
    {
        var clock = ChicagoNoonOn(2026, 8, 29);
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            DueDate = ChurchTimeZone.Today(clock).AddDays(7)
        };

        DueDateUrgencyCalculator.Calculate(workOrder, clock).ShouldBe(DueDateUrgency.None);
    }

    [Test]
    public void Calculate_WhenEditableDueDateChanges_UpdatesUrgency()
    {
        var clock = ChicagoNoonOn(2026, 8, 29);
        var today = ChurchTimeZone.Today(clock);

        var futureUrgency = DueDateUrgencyCalculator.Calculate(
            today.AddDays(1),
            WorkOrderStatus.Draft,
            clock);
        var todayUrgency = DueDateUrgencyCalculator.Calculate(
            today,
            WorkOrderStatus.Draft,
            clock);

        futureUrgency.ShouldBe(DueDateUrgency.None);
        todayUrgency.ShouldBe(DueDateUrgency.DueToday);
    }

    [Test]
    public void ComingSaturday_WhenTodayIsSaturday_ReturnsToday()
    {
        // 2026-08-29 is a Saturday
        var clock = ChicagoNoonOn(2026, 8, 29);
        ChurchTimeZone.ComingSaturday(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Test]
    public void ComingSaturday_WhenTodayIsFriday_ReturnsNextDay()
    {
        // 2026-08-28 is a Friday
        var clock = ChicagoNoonOn(2026, 8, 28);
        ChurchTimeZone.ComingSaturday(clock).ShouldBe(new DateOnly(2026, 8, 29));
    }

    private static FixedTimeProvider ChicagoNoonOn(int year, int month, int day)
    {
        var local = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, ChurchTimeZone.Chicago);
        return new FixedTimeProvider(new DateTimeOffset(utc, TimeSpan.Zero));
    }
}
