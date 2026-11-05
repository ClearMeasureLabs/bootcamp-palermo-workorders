using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class OpenWorkOrderCountQueryValidatorTests
{
    [Test]
    public void ShouldHaveNoErrors_OnDefaultQuery()
    {
        var validator = new OpenWorkOrderCountQueryValidator();

        var result = validator.TestValidate(new OpenWorkOrderCountQuery());

        result.IsValid.ShouldBeTrue();
    }
}
