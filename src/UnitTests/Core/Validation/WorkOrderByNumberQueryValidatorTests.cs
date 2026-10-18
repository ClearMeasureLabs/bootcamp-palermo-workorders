using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderByNumberQueryValidatorTests
{
    private WorkOrderByNumberQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WorkOrderByNumberQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenNumberIsValid()
    {
        var query = new WorkOrderByNumberQuery("WO-1234");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenNumberContainsSpace()
    {
        var query = new WorkOrderByNumberQuery("WO 1234");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Number)
            .WithErrorMessage("Work order number must not contain spaces.");
    }
}
