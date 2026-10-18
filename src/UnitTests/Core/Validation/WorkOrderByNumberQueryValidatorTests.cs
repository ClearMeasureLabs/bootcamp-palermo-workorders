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
    public void ShouldPass_WhenNumberIsSevenCharacters()
    {
        var query = new WorkOrderByNumberQuery("WO-1234");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenNumberIsEightCharacters()
    {
        var query = new WorkOrderByNumberQuery("WO-12345");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Number);
    }

    [Test]
    public void ShouldFail_WhenNumberIsEmpty()
    {
        var query = new WorkOrderByNumberQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Number);
    }
}
