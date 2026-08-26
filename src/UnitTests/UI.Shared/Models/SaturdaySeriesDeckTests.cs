using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class SaturdaySeriesDeckTests
{
    [Test]
    public void IsSaturdaySeries_WhenTenMatchingSaturdayMows_ReturnsTrue()
    {
        var rows = BuildSaturdaySeries(10, "Saturday mow", "gwillie", "Willie", "Aker");

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeTrue();
        SaturdaySeriesDeck.SeriesPromptText(rows).ShouldContain("Willie");
        SaturdaySeriesDeck.SeriesPromptText(rows).ShouldContain("10");
        SaturdaySeriesDeck.CardDateLabel(rows[0], 0, true).ShouldBe("Sat 1");
        SaturdaySeriesDeck.CardDateLabel(rows[9], 9, true).ShouldBe("Sat 10");
    }

    [Test]
    public void IsSaturdaySeries_WhenTitlesDiffer_ReturnsFalse()
    {
        var rows = BuildSaturdaySeries(3, "Saturday mow", "gwillie", "Willie", "Aker");
        rows[1] = CloneWithTitle(rows[1], "Sunday mow");

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeFalse();
        SaturdaySeriesDeck.SeriesPromptText(rows).ShouldBe(string.Empty);
    }

    [Test]
    public void IsSaturdaySeries_WhenDueDateNotSaturday_ReturnsFalse()
    {
        var rows = BuildSaturdaySeries(3, "Saturday mow", "gwillie", "Willie", "Aker");
        rows[0].WorkOrder.DueDate = new DateOnly(2026, 8, 28); // Friday

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeFalse();
    }

    [Test]
    public void IsSaturdaySeries_WhenFewerThanTwoRows_ReturnsFalse()
    {
        var rows = BuildSaturdaySeries(1, "Saturday mow", "gwillie", "Willie", "Aker");

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeFalse();
    }

    [Test]
    public void IsSaturdaySeries_WhenTitleBlank_ReturnsFalse()
    {
        var rows = BuildSaturdaySeries(3, "   ", "gwillie", "Willie", "Aker");

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeFalse();
    }

    [Test]
    public void IsSaturdaySeries_WhenDueDateMissing_ReturnsFalse()
    {
        var rows = BuildSaturdaySeries(3, "Saturday mow", "gwillie", "Willie", "Aker");
        rows[1].WorkOrder.DueDate = null;

        SaturdaySeriesDeck.IsSaturdaySeries(rows).ShouldBeFalse();
    }

    [Test]
    public void CardDateLabel_WhenNotSeries_UsesDueDateDisplay()
    {
        var row = new WorkOrderSearchResultRow
        {
            WorkOrder = new WorkOrder { Title = "Fix door", DueDate = new DateOnly(2026, 8, 26) },
            DueDateDisplay = "Aug 26, 2026"
        };

        SaturdaySeriesDeck.CardDateLabel(row, 0, false).ShouldBe("Aug 26, 2026");
    }

    private static WorkOrderSearchResultRow[] BuildSaturdaySeries(
        int count, string title, string userName, string first, string last)
    {
        var start = new DateOnly(2026, 8, 29); // Saturday
        var assignee = new Employee(userName, first, last, $"{userName}@example.com");
        var rows = new WorkOrderSearchResultRow[count];
        for (var i = 0; i < count; i++)
        {
            var due = start.AddDays(7 * i);
            rows[i] = new WorkOrderSearchResultRow
            {
                WorkOrder = new WorkOrder
                {
                    Number = $"WO-{i + 1:000}",
                    Title = title,
                    DueDate = due,
                    Assignee = assignee,
                    Status = WorkOrderStatus.Assigned
                },
                DueDateDisplay = due.ToString("MMM d, yyyy")
            };
        }

        return rows;
    }

    private static WorkOrderSearchResultRow CloneWithTitle(WorkOrderSearchResultRow source, string title)
    {
        return new WorkOrderSearchResultRow
        {
            WorkOrder = new WorkOrder
            {
                Number = source.Number,
                Title = title,
                DueDate = source.WorkOrder.DueDate,
                Assignee = source.Assignee,
                Status = source.Status
            },
            DueDateDisplay = source.DueDateDisplay
        };
    }
}
