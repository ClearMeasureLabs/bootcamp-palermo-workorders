using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderCountByAssigneeQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoParameters()
    {
        var validator = new WorkOrderCountByAssigneeQueryValidator();

        var result = validator.TestValidate(new WorkOrderCountByAssigneeQuery());

        result.IsValid.ShouldBeTrue();
    }
}
