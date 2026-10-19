using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class OpenWorkOrdersInRoomQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenRoomNumberIsProvided()
    {
        var validator = new OpenWorkOrdersInRoomQueryValidator();

        var result = validator.TestValidate(new OpenWorkOrdersInRoomQuery("101", "WO-1"));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenRoomNumberIsEmpty()
    {
        var validator = new OpenWorkOrdersInRoomQueryValidator();

        var result = validator.TestValidate(new OpenWorkOrdersInRoomQuery("", "WO-1"));

        result.ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }
}
