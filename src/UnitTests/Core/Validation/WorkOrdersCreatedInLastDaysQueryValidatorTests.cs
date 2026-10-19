using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrdersCreatedInLastDaysQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenDaysIsWithinOneToThirty()
    {
        var validator = new WorkOrdersCreatedInLastDaysQueryValidator();

        var result = validator.TestValidate(new WorkOrdersCreatedInLastDaysQuery(7));

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(31)]
    [TestCase(-1)]
    public void ShouldFail_WhenDaysIsOutsideOneToThirty(int days)
    {
        var validator = new WorkOrdersCreatedInLastDaysQueryValidator();

        var result = validator.TestValidate(new WorkOrdersCreatedInLastDaysQuery(days));

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [TestCase(1)]
    [TestCase(30)]
    public void ShouldPass_WhenDaysIsAtBoundary(int days)
    {
        var validator = new WorkOrdersCreatedInLastDaysQueryValidator();

        var result = validator.TestValidate(new WorkOrdersCreatedInLastDaysQuery(days));

        result.IsValid.ShouldBeTrue();
    }
}
