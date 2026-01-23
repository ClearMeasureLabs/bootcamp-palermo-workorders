using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderValidationExceptionTests
{
    [Test]
    public void ShouldCreateExceptionWithSingleError()
    {
        var exception = new WorkOrderValidationException("Title is required.");

        exception.ValidationErrors.Count.ShouldBe(1);
        exception.ValidationErrors.ShouldContain("Title is required.");
        exception.Message.ShouldContain("Title is required.");
    }

    [Test]
    public void ShouldCreateExceptionWithMultipleErrors()
    {
        var errors = new[] { "Title is required.", "Description is required." };
        var exception = new WorkOrderValidationException(errors);

        exception.ValidationErrors.Count.ShouldBe(2);
        exception.ValidationErrors.ShouldContain("Title is required.");
        exception.ValidationErrors.ShouldContain("Description is required.");
        exception.Message.ShouldContain("Title is required.");
        exception.Message.ShouldContain("Description is required.");
    }

    [Test]
    public void ShouldHaveCorrectMessageFormat()
    {
        var exception = new WorkOrderValidationException("Test error");

        exception.Message.ShouldBe("Work order validation failed: Test error");
    }

    [Test]
    public void ShouldJoinMultipleErrorsWithSemicolon()
    {
        var errors = new[] { "Error 1", "Error 2" };
        var exception = new WorkOrderValidationException(errors);

        exception.Message.ShouldBe("Work order validation failed: Error 1; Error 2");
    }
}
