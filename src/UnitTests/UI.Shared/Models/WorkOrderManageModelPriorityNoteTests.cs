using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelPriorityNoteTests
{
    [Test]
    public void ShouldAllowMissingPriorityNote()
    {
        var model = ValidModel();
        model.PriorityNote = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAllowEmptyPriorityNote()
    {
        var model = ValidModel();
        model.PriorityNote = "";

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptPriorityNoteAtMaxLength()
    {
        var model = ValidModel();
        model.PriorityNote = new string('P', WorkOrder.PriorityNoteMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectPriorityNoteLongerThanMaxLength()
    {
        var model = ValidModel();
        model.PriorityNote = new string('P', WorkOrder.PriorityNoteMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.PriorityNote)));
    }

    [Test]
    public void ShouldRejectPriorityNoteLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.PriorityNote = new string('P', WorkOrder.PriorityNoteMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.PriorityNote))
            && r.ErrorMessage == "Priority note cannot exceed 200 characters.");
    }

    [Test]
    public void PriorityNoteMaxLength_ShouldBe200()
    {
        WorkOrder.PriorityNoteMaxLength.ShouldBe(200);
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
