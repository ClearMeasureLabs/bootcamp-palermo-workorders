using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTitleTests
{
    [Test]
    public void ShouldRejectNullTitle()
    {
        var model = ValidModel();
        model.Title = null;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldRejectEmptyTitle()
    {
        var model = ValidModel();
        model.Title = string.Empty;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldRejectWhitespaceOnlyTitle()
    {
        var model = ValidModel();
        model.Title = "   ";

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldAcceptNonEmptyTitle()
    {
        var model = ValidModel();
        model.Title = "Title";

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectNullTitle_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Title = null;

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe("The Title field is required.");
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
