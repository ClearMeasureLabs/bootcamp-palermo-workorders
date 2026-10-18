using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class OverdueWorkOrderCountQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasAsOfDate()
    {
        var validator = new OverdueWorkOrderCountQueryValidator();

        var result = validator.TestValidate(new OverdueWorkOrderCountQuery(new DateOnly(2024, 6, 15)));

        result.IsValid.ShouldBeTrue();
    }
}
