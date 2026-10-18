using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class RoleByNameQueryValidatorTests
{
    private RoleByNameQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RoleByNameQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenNameIsProvided()
    {
        var query = new RoleByNameQuery("Admin");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenNameIsEmpty()
    {
        var query = new RoleByNameQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenNameIsWhitespace()
    {
        var query = new RoleByNameQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
