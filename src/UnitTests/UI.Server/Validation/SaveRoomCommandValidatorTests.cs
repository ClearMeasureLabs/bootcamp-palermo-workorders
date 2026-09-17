using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Validation;

[TestFixture]
public class SaveRoomCommandValidatorTests
{
    private SaveRoomCommandValidator validator = null!;

    [SetUp]
    public void SetUp()
    {
        validator = new SaveRoomCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenRoomHasValidName()
    {
        var command = new SaveRoomCommand(new Room { Id = Guid.NewGuid(), Name = "Room A" });

        var result = validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldHaveValidationError_WhenRoomIsNull()
    {
        var command = new SaveRoomCommand(null!);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Room);
    }

    [Test]
    public void ShouldHaveValidationError_WhenRoomNameIsEmpty()
    {
        var command = new SaveRoomCommand(new Room { Id = Guid.NewGuid(), Name = string.Empty });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Room.Name);
    }

    [Test]
    public void ShouldHaveValidationError_WhenRoomNameExceedsMaxLength()
    {
        var name = new string('A', Room.NameMaxLength + 1);
        var command = new SaveRoomCommand(new Room { Id = Guid.NewGuid(), Name = name });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Room.Name);
    }
}
