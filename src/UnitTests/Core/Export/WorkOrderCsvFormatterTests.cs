using ClearMeasure.Bootcamp.Core.Export;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Export;

[TestFixture]
public class WorkOrderCsvFormatterTests
{
    [Test]
    public void ShouldIncludeHeader_WhenFormattingEmptyList()
    {
        var csv = WorkOrderCsvFormatter.Format([]);

        csv.ShouldStartWith(WorkOrderCsvFormatter.Header);
        csv.TrimEnd().ShouldBe(WorkOrderCsvFormatter.Header);
    }

    [Test]
    public void ShouldEscapeQuotesAndCommas_WhenFieldNeedsQuoting()
    {
        WorkOrderCsvFormatter.Escape("plain").ShouldBe("plain");
        WorkOrderCsvFormatter.Escape("a,b").ShouldBe("\"a,b\"");
        WorkOrderCsvFormatter.Escape("He said \"hi\"").ShouldBe("\"He said \"\"hi\"\"\"");
        WorkOrderCsvFormatter.Escape("line\nbreak").ShouldBe("\"line\nbreak\"");
        WorkOrderCsvFormatter.Escape(null).ShouldBe(string.Empty);
    }

    [Test]
    public void ShouldFormatRow_WhenWorkOrderHasValues()
    {
        var workOrder = new WorkOrder
        {
            Number = "WO-1",
            Title = "Fix, leak",
            Status = WorkOrderStatus.InProgress,
            Assignee = new Employee("jdoe", "Jane", "Doe", "j@example.com"),
            DueDate = new DateOnly(2026, 10, 17)
        };

        var csv = WorkOrderCsvFormatter.Format([workOrder]);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[0].TrimEnd('\r').ShouldBe(WorkOrderCsvFormatter.Header);
        lines[1].TrimEnd('\r').ShouldBe("WO-1,\"Fix, leak\",In Progress,jdoe,2026-10-17");
    }
}
