using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class StaleWorkOrdersQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenDaysIsWithinRange()
    {
        var validator = new StaleWorkOrdersQueryValidator();

        var result = validator.TestValidate(new StaleWorkOrdersQuery(14));

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(91)]
    public void ShouldFail_WhenDaysIsOutsideRange(int days)
    {
        var validator = new StaleWorkOrdersQueryValidator();

        var result = validator.TestValidate(new StaleWorkOrdersQuery(days));

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [TestCase(1)]
    [TestCase(90)]
    public void ShouldPass_WhenDaysIsAtBoundary(int days)
    {
        var validator = new StaleWorkOrdersQueryValidator();

        var result = validator.TestValidate(new StaleWorkOrdersQuery(days));

        result.IsValid.ShouldBeTrue();
    }
}
