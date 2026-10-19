using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WorkOrderCsvWriterTests
{
    [Test]
    public void Escape_Should_ReturnEmpty_When_ValueIsNull()
    {
        WorkOrderCsvWriter.Escape(null).ShouldBe(string.Empty);
    }

    [Test]
    public void Escape_Should_ReturnUnchanged_When_NoSpecialCharacters()
    {
        WorkOrderCsvWriter.Escape("plain").ShouldBe("plain");
    }

    [Test]
    public void Escape_Should_QuoteAndDoubleQuotes_When_ValueContainsComma()
    {
        WorkOrderCsvWriter.Escape("a,b").ShouldBe("\"a,b\"");
    }

    [Test]
    public void Escape_Should_QuoteAndDoubleQuotes_When_ValueContainsQuote()
    {
        WorkOrderCsvWriter.Escape("say \"hi\"").ShouldBe("\"say \"\"hi\"\"\"");
    }

    [Test]
    public void Escape_Should_Quote_When_ValueContainsNewline()
    {
        WorkOrderCsvWriter.Escape("line1\nline2").ShouldBe("\"line1\nline2\"");
    }

    [Test]
    public void Write_Should_IncludeHeaderAndEscapedRow()
    {
        var creator = new Employee("alice", "Alice", "A", "a@test.com");
        var assignee = new Employee("bob", "Bob", "B", "b@test.com");
        var order = new WorkOrder
        {
            Number = "WO-1",
            Title = "Fix, \"door\"",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Assignee = assignee,
            RoomNumber = "101",
            DueDate = new DateOnly(2026, 3, 15)
        };

        var csv = WorkOrderCsvWriter.Write([order]);

        csv.ShouldStartWith("Number,Title,Status,Creator,Assignee,Room,DueDate");
        csv.ShouldContain("WO-1");
        csv.ShouldContain("\"Fix, \"\"door\"\"\"");
        csv.ShouldContain("Draft");
        csv.ShouldContain("alice");
        csv.ShouldContain("bob");
        csv.ShouldContain("101");
        csv.ShouldContain("2026-03-15");
    }
}
