using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class WorkOrderBulkImportParseResultTests
{
    [Test]
    public void Fail_WithMessage_ShouldSetError()
    {
        var result = WorkOrderBulkImportParseResult.Fail("x");

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("x");
        result.Rows.ShouldBeEmpty();
    }

    [Test]
    public void Fail_WithNull_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => WorkOrderBulkImportParseResult.Fail(null!));
    }

    [Test]
    public void Fail_WithWhitespace_ShouldThrowArgumentException()
    {
        Should.Throw<ArgumentException>(() => WorkOrderBulkImportParseResult.Fail("  "));
    }
}
