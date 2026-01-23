using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class SaveDraftCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(order.CreatedDate, Is.Not.Null);
    }

    [Test]
    public void ShouldReturnValidationErrorWhenTitleIsEmpty()
    {
        var order = new WorkOrder { Title = "", Description = "Valid description" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(1);
        errors.ShouldContain("Title is required.");
    }

    [Test]
    public void ShouldReturnValidationErrorWhenDescriptionIsEmpty()
    {
        var order = new WorkOrder { Title = "Valid title", Description = "" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(1);
        errors.ShouldContain("Description is required.");
    }

    [Test]
    public void ShouldReturnValidationErrorsWhenBothTitleAndDescriptionAreEmpty()
    {
        var order = new WorkOrder { Title = "", Description = "" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(2);
        errors.ShouldContain("Title is required.");
        errors.ShouldContain("Description is required.");
    }

    [Test]
    public void ShouldReturnNoValidationErrorsWhenTitleAndDescriptionAreValid()
    {
        var order = new WorkOrder { Title = "Valid title", Description = "Valid description" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldReturnValidationErrorWhenTitleIsWhitespace()
    {
        var order = new WorkOrder { Title = "   ", Description = "Valid description" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(1);
        errors.ShouldContain("Title is required.");
    }

    [Test]
    public void ShouldReturnValidationErrorWhenTitleIsNull()
    {
        var order = new WorkOrder { Title = null, Description = "Valid description" };
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var errors = command.Validate();

        errors.Count.ShouldBe(1);
        errors.ShouldContain("Title is required.");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new SaveDraftCommand(order, employee);
    }
}