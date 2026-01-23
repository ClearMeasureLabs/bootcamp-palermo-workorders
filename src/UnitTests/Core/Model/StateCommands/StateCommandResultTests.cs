using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class StateCommandResultTests
{
    [Test]
    public void ShouldReturnIsSuccessTrueWhenNoValidationErrors()
    {
        var workOrder = new WorkOrder { Title = "Test", Description = "Test" };
        var result = new StateCommandResult(workOrder, "Save", "Success");

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnIsSuccessTrueWhenValidationErrorsIsNull()
    {
        var workOrder = new WorkOrder { Title = "Test", Description = "Test" };
        var result = new StateCommandResult(workOrder, "Save", "Success", null);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnIsSuccessTrueWhenValidationErrorsIsEmptyList()
    {
        var workOrder = new WorkOrder { Title = "Test", Description = "Test" };
        var result = new StateCommandResult(workOrder, "Save", "Success", new List<string>());

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnIsSuccessFalseWhenValidationErrorsExist()
    {
        var workOrder = new WorkOrder();
        var errors = new List<string> { "Title is required." };
        var result = new StateCommandResult(workOrder, "Save", "", errors);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void ShouldCreateFailureResultWithValidationErrors()
    {
        var workOrder = new WorkOrder();
        var errors = new List<string> { "Title is required.", "Description is required." };

        var result = StateCommandResult.Failure(workOrder, errors);

        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeNull();
        result.ValidationErrors!.Count.ShouldBe(2);
        result.ValidationErrors.ShouldContain("Title is required.");
        result.ValidationErrors.ShouldContain("Description is required.");
        result.WorkOrder.ShouldBe(workOrder);
    }
}
