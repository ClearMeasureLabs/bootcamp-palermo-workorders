using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeeCountQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoParameters()
    {
        var validator = new EmployeeCountQueryValidator();

        var result = validator.TestValidate(new EmployeeCountQuery());

        result.IsValid.ShouldBeTrue();
    }
}
