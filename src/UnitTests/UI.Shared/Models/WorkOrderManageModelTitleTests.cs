using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
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
        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe("The Title field is required.");
    }

    [Test]
    public void ShouldRejectEmptyTitle()
    {
        var model = ValidModel();
        model.Title = string.Empty;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe("The Title field is required.");
    }

    [Test]
    public void ShouldRejectWhitespaceTitle()
    {
        var model = ValidModel();
        model.Title = "   ";

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe("The Title field is required.");
    }

    [Test]
    public void ShouldAllowNonEmptyTitle()
    {
        var model = ValidModel();

        var results = Validate(model);

        results.ShouldBeEmpty();
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
