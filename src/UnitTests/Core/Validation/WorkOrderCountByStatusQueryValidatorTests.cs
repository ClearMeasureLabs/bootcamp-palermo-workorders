using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderCountByStatusQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoParameters()
    {
        var validator = new WorkOrderCountByStatusQueryValidator();

        var result = validator.TestValidate(new WorkOrderCountByStatusQuery());

        result.IsValid.ShouldBeTrue();
    }
}
