using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class WorkOrderBulkImportParseResultTests
{
    [Test]
    public void ShouldOk_WithRows_SetSuccessAndPreserveRows()
    {
        var rows = new List<WorkOrderBulkImportRow>
        {
            new(1, "Title", "Description", "creator", "101")
        };

        var result = WorkOrderBulkImportParseResult.Ok(rows);

        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.Rows.ShouldBe(rows);
    }

    [Test]
    public void ShouldFail_WithError_SetFailureAndEmptyRows()
    {
        var result = WorkOrderBulkImportParseResult.Fail("bad row");

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("bad row");
        result.Rows.ShouldBeEmpty();
    }
}
