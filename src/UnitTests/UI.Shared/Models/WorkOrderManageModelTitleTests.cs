using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTitleTests
{
    [Test]
    public void ShouldAcceptTitleAtMaxLength()
    {
        var model = ValidModel();
        model.Title = new string('T', WorkOrder.TitleMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectTitleLongerThanMaxLength()
    {
        var model = ValidModel();
        model.Title = new string('T', WorkOrder.TitleMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void TitleMaxLength_ShouldBe320()
    {
        WorkOrder.TitleMaxLength.ShouldBe(320);
    }

    [Test]
    public void ShouldRejectEmptyTitle()
    {
        var model = ValidModel();
        model.Title = "";

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
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
