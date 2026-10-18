using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeeByEmailQueryValidatorTests
{
    private EmployeeByEmailQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new EmployeeByEmailQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenEmailIsNotEmpty()
    {
        var query = new EmployeeByEmailQuery("user@example.com");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenEmailIsEmpty()
    {
        var query = new EmployeeByEmailQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void ShouldFail_WhenEmailIsWhitespace()
    {
        var query = new EmployeeByEmailQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
