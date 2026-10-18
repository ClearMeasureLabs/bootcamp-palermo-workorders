using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeesByRoleQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenRoleNameIsNotEmpty()
    {
        var validator = new EmployeesByRoleQueryValidator();

        var result = validator.TestValidate(new EmployeesByRoleQuery("Manager"));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenRoleNameIsEmpty()
    {
        var validator = new EmployeesByRoleQueryValidator();

        var result = validator.TestValidate(new EmployeesByRoleQuery(string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.RoleName);
    }
}
