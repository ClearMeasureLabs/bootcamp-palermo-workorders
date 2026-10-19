using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class DraftToCancelledCommandValidatorTests
{
    [Test]
    public void ShouldPass_WhenCommandHasWorkOrderAndUser()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = employee };
        var validator = new DraftToCancelledCommandValidator();

        var result = validator.TestValidate(new DraftToCancelledCommand(order, employee));

        result.IsValid.ShouldBeTrue();
    }
}
