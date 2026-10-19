using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class AssignedOpenWorkOrderCountQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenUserNameIsPresent()
    {
        var validator = new AssignedOpenWorkOrderCountQueryValidator();

        var result = validator.TestValidate(new AssignedOpenWorkOrderCountQuery("jdoe"));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUserNameIsEmpty()
    {
        var validator = new AssignedOpenWorkOrderCountQueryValidator();

        var result = validator.TestValidate(new AssignedOpenWorkOrderCountQuery(""));

        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
