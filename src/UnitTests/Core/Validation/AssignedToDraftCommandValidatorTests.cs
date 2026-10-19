using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class AssignedToDraftCommandValidatorTests
{
    [Test]
    public void ShouldPass_WhenCommandHasNoExtraRules()
    {
        var validator = new AssignedToDraftCommandValidator();
        var order = new WorkOrder { Status = WorkOrderStatus.Assigned };
        var user = new Employee();

        var result = validator.TestValidate(new AssignedToDraftCommand(order, user));

        result.IsValid.ShouldBeTrue();
    }
}
