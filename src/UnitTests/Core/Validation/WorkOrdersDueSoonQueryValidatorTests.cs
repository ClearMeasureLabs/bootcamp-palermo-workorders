using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrdersDueSoonQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenDaysIsWithinRange()
    {
        var validator = new WorkOrdersDueSoonQueryValidator();

        var result = validator.TestValidate(new WorkOrdersDueSoonQuery(7));

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(31)]
    public void ShouldFail_WhenDaysIsOutsideRange(int days)
    {
        var validator = new WorkOrdersDueSoonQueryValidator();

        var result = validator.TestValidate(new WorkOrdersDueSoonQuery(days));

        result.ShouldHaveValidationErrorFor(query => query.Days);
    }
}
