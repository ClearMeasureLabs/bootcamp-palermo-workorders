using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AssignedToDraftCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var creator = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Assignee = new Employee()
        };

        var command = new AssignedToDraftCommand(order, creator);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var creator = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = new Employee()
        };

        var command = new AssignedToDraftCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var creator = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = new Employee()
        };

        var command = new AssignedToDraftCommand(order, creator);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var creator = new Employee();
        var assignee = new Employee();
        var order = new WorkOrder()
        {
            Number = "123",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee,
            AssignedDate = DateTime.UtcNow
        };

        var command = new AssignedToDraftCommand(order, creator);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(order.Assignee, Is.Null);
        Assert.That(order.AssignedDate, Is.Null);
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new AssignedToDraftCommand(order, employee);
    }
}
