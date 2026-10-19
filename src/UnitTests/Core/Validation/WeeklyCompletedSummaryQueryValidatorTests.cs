using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WeeklyCompletedSummaryQueryValidatorTests
{
    [Test]
    public void Should_Validate_EmptyQuery_AsValid()
    {
        var validator = new WeeklyCompletedSummaryQueryValidator();

        var result = validator.Validate(new WeeklyCompletedSummaryQuery());

        result.IsValid.ShouldBeTrue();
    }
}
