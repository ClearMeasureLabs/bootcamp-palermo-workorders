using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeeByUserNameQueryValidatorTests
{
    private EmployeeByUserNameQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new EmployeeByUserNameQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenUsernameIsNotEmpty()
    {
        var query = new EmployeeByUserNameQuery("jsmith");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUsernameIsEmpty()
    {
        var query = new EmployeeByUserNameQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void ShouldFail_WhenUsernameIsWhitespace()
    {
        var query = new EmployeeByUserNameQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
