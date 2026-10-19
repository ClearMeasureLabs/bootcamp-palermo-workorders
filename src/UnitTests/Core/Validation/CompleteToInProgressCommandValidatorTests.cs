using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CompleteToInProgressCommandValidatorTests
{
    [Test]
    public void ShouldPass_WhenCommandHasNoRules()
    {
        var validator = new CompleteToInProgressCommandValidator();
        var workOrder = new WorkOrder();
        var user = new Employee();

        var result = validator.TestValidate(new CompleteToInProgressCommand(workOrder, user));

        result.IsValid.ShouldBeTrue();
    }
}
