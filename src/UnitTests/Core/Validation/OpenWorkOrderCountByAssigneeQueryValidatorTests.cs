using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class OpenWorkOrderCountByAssigneeQueryValidatorTests
{
    private OpenWorkOrderCountByAssigneeQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new OpenWorkOrderCountByAssigneeQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenUsernameIsNotEmpty()
    {
        var query = new OpenWorkOrderCountByAssigneeQuery("jsmith");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUsernameIsEmpty()
    {
        var query = new OpenWorkOrderCountByAssigneeQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
