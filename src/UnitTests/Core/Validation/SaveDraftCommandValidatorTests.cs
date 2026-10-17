using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class SaveDraftCommandValidatorTests
{
    private SaveDraftCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveDraftCommandValidator();
    }

    [Test]
    public void ShouldFail_WhenDueDateIsYesterday()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var command = CreateCommand(today.AddDays(-1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.DueDate);
    }

    [Test]
    public void ShouldPass_WhenDueDateIsToday()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var command = CreateCommand(today);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenDueDateIsNull()
    {
        var command = CreateCommand(null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    private static SaveDraftCommand CreateCommand(DateOnly? dueDate)
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Creator = employee,
            DueDate = dueDate
        };
        return new SaveDraftCommand(order, employee);
    }
}
