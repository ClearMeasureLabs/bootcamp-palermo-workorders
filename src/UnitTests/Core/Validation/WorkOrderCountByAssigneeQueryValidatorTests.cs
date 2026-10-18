using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderCountByAssigneeQueryValidatorTests
{
    private WorkOrderCountByAssigneeQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WorkOrderCountByAssigneeQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenUsernameIsNotEmpty()
    {
        var query = new WorkOrderCountByAssigneeQuery("jsmith");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenUsernameIsEmpty()
    {
        var query = new WorkOrderCountByAssigneeQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void ShouldFail_WhenUsernameIsWhitespace()
    {
        var query = new WorkOrderCountByAssigneeQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
