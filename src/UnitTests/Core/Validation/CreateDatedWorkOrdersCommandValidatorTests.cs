using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateDatedWorkOrdersCommandValidatorTests
{
    private CreateDatedWorkOrdersCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateDatedWorkOrdersCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenAllFieldsPopulated()
    {
        var command = new CreateDatedWorkOrdersCommand("tlovejoy", "gwillie", "Mow", "Mow the lawn",
            [new DateOnly(2026, 9, 19)]);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenCreatorUsernameIsEmpty()
    {
        var command = new CreateDatedWorkOrdersCommand("", "gwillie", "Mow", "Mow the lawn",
            [new DateOnly(2026, 9, 19)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CreatorUsername);
    }

    [Test]
    public void ShouldFail_WhenDueDatesIsEmpty()
    {
        var command = new CreateDatedWorkOrdersCommand("tlovejoy", "gwillie", "Mow", "Mow the lawn", []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDates);
    }

    [Test]
    public void ShouldFail_WhenDueDatesIsNull()
    {
        var command = new CreateDatedWorkOrdersCommand("tlovejoy", "gwillie", "Mow", "Mow the lawn", null!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDates);
    }

    [Test]
    public void ShouldFail_WhenDueDatesExceedsMaximumBatchSize()
    {
        var dueDates = Enumerable.Range(0, CreateDatedWorkOrdersCommand.MaximumBatchSize + 1)
            .Select(offset => new DateOnly(2026, 9, 19).AddDays(offset))
            .ToList();
        var command = new CreateDatedWorkOrdersCommand("tlovejoy", "gwillie", "Mow", "Mow the lawn", dueDates);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDates);
    }
}
