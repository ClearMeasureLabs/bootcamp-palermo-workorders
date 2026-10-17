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
        using var ms = new MemoryStream("Title,Description\na,b\n"u8.ToArray());
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("CreatorUsername");
    }

    [Test]
    public void ShouldParseRows_WhenHeaderAndDataPresent()
    {
        var csv = "Title,Description,CreatorUsername,RoomNumber\n"
                  + "Fix leak,Under sink,u1,101\n"
                  + "\"Title, with comma\",Plain desc,u2,\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].Title.ShouldBe("Fix leak");
        result.Rows[0].Description.ShouldBe("Under sink");
        result.Rows[0].CreatorUsername.ShouldBe("u1");
        result.Rows[0].RoomNumber.ShouldBe("101");
        result.Rows[1].Title.ShouldBe("Title, with comma");
        result.Rows[1].Description.ShouldBe("Plain desc");
        result.Rows[1].CreatorUsername.ShouldBe("u2");
        result.Rows[1].RoomNumber.ShouldBeNull();
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
    public void ShouldParseMultilineQuotedField_WhenLogicalLineSpansRows()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "T,\"Line one\nLine two\",u\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Description.ShouldBe("Line one\nLine two");
    }

    [Test]
    public void ShouldSucceedWithEmptyRows_WhenHeaderOnly()
    {
        using var ms = new MemoryStream("Title,Description,CreatorUsername\n"u8.ToArray());
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSkipBlankDataLines_WhenWhitespaceOnlyRowsPresent()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "   \n"
                  + "Keep,Desc,user1\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Title.ShouldBe("Keep");
    }

    [Test]
    public void ShouldFail_WhenRequiredHeaderIsDuplicated()
    {
        var csv = "Title,Description,Title,CreatorUsername\nFix,Desc,user1\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);
        result.Success.ShouldBeFalse();
        Message(result).ShouldBe("Duplicate column \"Title\".");
    }

    [Test]
    public void ShouldFail_WhenRequiredHeaderIsDuplicatedIgnoringCase()
    {
        var csv = "Title,Description,title,CreatorUsername\nFix,Desc,user1\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);
        result.Success.ShouldBeFalse();
        Message(result).ShouldBe("Duplicate column \"Title\".");
    }

    private static string Message(WorkOrderBulkImportParseResult result)
    {
        foreach (var property in typeof(WorkOrderBulkImportParseResult).GetProperties())
        {
            if (property.GetValue(result) is string { Length: > 0 } value)
            {
                return value;
            }
        }

        return "";
    }
}
