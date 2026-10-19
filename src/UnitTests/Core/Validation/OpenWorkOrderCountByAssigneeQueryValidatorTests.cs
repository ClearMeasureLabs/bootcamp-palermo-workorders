using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class OpenWorkOrderCountByAssigneeQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoParameters()
    {
        var validator = new OpenWorkOrderCountByAssigneeQueryValidator();

        var result = validator.TestValidate(new OpenWorkOrderCountByAssigneeQuery());

        result.IsValid.ShouldBeTrue();
    }
}
