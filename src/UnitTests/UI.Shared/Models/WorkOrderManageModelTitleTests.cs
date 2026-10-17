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
        var model = new WorkOrderManageModel { Title = new string('T', WorkOrder.TitleMaxLength), Description = "Description" };
        Validate(model).ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectTitleLongerThanMaxLength()
    {
        var model = new WorkOrderManageModel { Title = new string('T', WorkOrder.TitleMaxLength + 1), Description = "Description" };
        Validate(model).ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void TitleMaxLength_ShouldBe1000()
    {
        WorkOrder.TitleMaxLength.ShouldBe(1000);
    }

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
