using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToCancelledCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Assigned,
            Creator = employee,
            Assignee = employee
        };

        var command = new InProgressToCancelledCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = employee
        };

        var command = new InProgressToCancelledCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = employee
        };

        var command = new InProgressToCancelledCommand(order, employee);
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
            Creator = employee,
            Assignee = employee,
            AssignedDate = DateTime.UtcNow
        };

        var command = new InProgressToCancelledCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Cancelled));
        Assert.That(order.Assignee, Is.Null);
        Assert.That(order.AssignedDate, Is.Null);
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToCancelledCommand(order, employee);
    }
}
