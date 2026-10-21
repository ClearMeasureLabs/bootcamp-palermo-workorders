using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelCustomerRepTests
{
    [Test]
    public void ShouldAllowMissingCustomerRep()
    {
        var model = ValidModel();
        model.CustomerRep = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptCustomerRepAtMaxLength()
    {
        var model = ValidModel();
        model.CustomerRep = new string('C', WorkOrder.CustomerRepMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectCustomerRepLongerThanMaxLength()
    {
        var model = ValidModel();
        model.CustomerRep = new string('C', WorkOrder.CustomerRepMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.CustomerRep)));
    }

    [Test]
    public void CustomerRepMaxLength_ShouldBe50()
    {
        WorkOrder.CustomerRepMaxLength.ShouldBe(50);
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
