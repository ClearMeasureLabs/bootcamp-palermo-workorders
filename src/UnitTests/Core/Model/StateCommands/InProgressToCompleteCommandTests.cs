using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToCompleteCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Complete,
            Assignee = employee
        };

        var command = new InProgressToCompleteCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };

        var command = new InProgressToCompleteCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };

        var command = new InProgressToCompleteCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Number = "123",
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };

        var command = new InProgressToCompleteCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(order.CompletedDate, Is.Not.Null);
    }

    [Test]
    public void ShouldRaiseWorkOrderCompletedEvent()
    {
        var employee = new Employee("jdoe", "Jane", "Doe", "jane@example.com");
        var order = new WorkOrder()
        {
            Number = "WO-99",
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };

        var command = new InProgressToCompleteCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(command.StateTransitionEvent, Is.TypeOf<WorkOrderCompletedEvent>());
        var completed = (WorkOrderCompletedEvent)command.StateTransitionEvent!;
        Assert.That(completed.Number, Is.EqualTo("WO-99"));
        Assert.That(completed.AssigneeName, Is.EqualTo(employee.GetFullName()));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToCompleteCommand(order, employee);
    }
}
