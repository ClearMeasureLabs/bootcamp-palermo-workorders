using System.Text;
using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class WorkOrderBulkImportCsvParserTests
{
    [Test]
    public void ShouldFail_WhenCsvIsEmpty()
    {
        using var ms = new MemoryStream();
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("CSV is empty.");
    }

    [Test]
    public void ShouldFail_WhenRequiredColumnMissing()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("Title,Description\na,b\n"));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("CreatorUsername");
    }

    [Test]
    public void ShouldParseRows_WhenHeaderAndDataPresent()
    {
        var csv = "Title,Description,CreatorUsername,Instructions,RoomNumber\n"
                  + "Fix leak,Under sink,u1,Turn off water,101\n"
                  + "\"Title, with comma\",Plain desc,u2,,\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].Title.ShouldBe("Fix leak");
        result.Rows[0].Description.ShouldBe("Under sink");
        result.Rows[0].CreatorUsername.ShouldBe("u1");
        result.Rows[0].Instructions.ShouldBe("Turn off water");
        result.Rows[0].RoomNumber.ShouldBe("101");
        result.Rows[1].Title.ShouldBe("Title, with comma");
        result.Rows[1].Description.ShouldBe("Plain desc");
        result.Rows[1].CreatorUsername.ShouldBe("u2");
        result.Rows[1].Instructions.ShouldBeNull();
        result.Rows[1].RoomNumber.ShouldBeNull();
    }

    [Test]
    public void ShouldParseOptionalInstructionsColumn_WhenPresent()
    {
        var csv = "Title,Description,CreatorUsername,Instructions\nT,D,u,Step one\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Instructions.ShouldBe("Step one");
    }

    [Test]
    public void ShouldParseInstructionsColumn_WhenBetweenDescriptionAndCreator()
    {
        var csv = "Title,Description,Instructions,CreatorUsername,RoomNumber\n"
                  + "T1,D1,Bring tools,u1,201\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Instructions.ShouldBe("Bring tools");
        result.Rows[0].RoomNumber.ShouldBe("201");
    }

    [Test]
    public void ShouldUnescapeDoubledQuotes_WhenInsideQuotedField()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "T,\"He said \"\"hi\"\"\",u\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Description.ShouldBe("He said \"hi\"");
    }

    [Test]
    public void ShouldParseTwoDataRows_WhenMatchesIntegrationTestCsv()
    {
        var csv = "Title,Description,CreatorUsername,RoomNumber\n"
                  + "First,Desc one,bulk-user,1A\n"
                  + "Second,Desc two,bulk-user,\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldSkipBlankLines_WhenPresent()
    {
        var csv = "Title,Description,CreatorUsername\n\n  \nT,D,u\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Title.ShouldBe("T");
    }
}
