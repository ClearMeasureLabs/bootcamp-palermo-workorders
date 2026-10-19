using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderSummarizeQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasTitleDescriptionAndInstructions()
    {
        var validator = new WorkOrderSummarizeQueryValidator();

        var result = validator.TestValidate(
            new WorkOrderSummarizeQuery("Title", "Description", "Instructions"));

        result.IsValid.ShouldBeTrue();
    }
}
