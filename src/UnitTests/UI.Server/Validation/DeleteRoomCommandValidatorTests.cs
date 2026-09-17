using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Validation;

[TestFixture]
public class DeleteRoomCommandValidatorTests
{
    private DeleteRoomCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new DeleteRoomCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenRoomIdIsValid()
    {
        var command = new DeleteRoomCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldHaveValidationError_WhenRoomIdIsEmpty()
    {
        var command = new DeleteRoomCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RoomId);
    }
}
