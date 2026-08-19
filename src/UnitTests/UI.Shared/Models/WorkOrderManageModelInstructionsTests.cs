using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelInstructionsTests
{
    [Test]
    public void ShouldAllowMissingInstructions()
    {
        var model = ValidModel();
        model.Instructions = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAllowEmptyInstructions()
    {
        var model = ValidModel();
        model.Instructions = string.Empty;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptInstructionsAtMaxLength()
    {
        var model = ValidModel();
        model.Instructions = new string('x', WorkOrder.InstructionsMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectInstructionsLongerThanMaxLength()
    {
        var model = ValidModel();
        model.Instructions = new string('x', WorkOrder.InstructionsMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Instructions)));
    }

    [Test]
    public void ShouldRejectInstructionsLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Instructions = new string('x', WorkOrder.InstructionsMaxLength + 1);

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Instructions)))
            .ErrorMessage.ShouldBe("Instructions cannot exceed 4000 characters.");
    }

    private static WorkOrderManageModel ValidModel()
    {
        return new WorkOrderManageModel
        {
            Title = "Title",
            Description = "Description"
        };
    }

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
