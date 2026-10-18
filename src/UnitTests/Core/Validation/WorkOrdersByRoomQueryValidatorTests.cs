using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrdersByRoomQueryValidatorTests
{
    private WorkOrdersByRoomQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WorkOrdersByRoomQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenRoomNumberIsNotEmpty()
    {
        var query = new WorkOrdersByRoomQuery("CR-101");

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenRoomNumberIsEmpty()
    {
        var query = new WorkOrdersByRoomQuery(string.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }

    [Test]
    public void ShouldFail_WhenRoomNumberIsWhitespace()
    {
        var query = new WorkOrdersByRoomQuery("   ");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }
}
