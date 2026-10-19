using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class EmployeeWorkloadQueryValidatorTests
{
    private EmployeeWorkloadQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new EmployeeWorkloadQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenUsernameIsNotEmpty()
    {
        var query = new EmployeeWorkloadQuery("jsmith");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUsernameIsEmpty()
    {
        var query = new EmployeeWorkloadQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void ShouldFail_WhenUsernameIsWhitespace()
    {
        var query = new EmployeeWorkloadQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
