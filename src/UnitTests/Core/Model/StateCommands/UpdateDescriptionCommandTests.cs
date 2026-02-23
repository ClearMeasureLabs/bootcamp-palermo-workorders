using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class UpdateDescriptionCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var creator = new Employee();
        order.Creator = creator;

        var command = new UpdateDescriptionCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValidWhenCurrentUserIsCreator()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var creator = new Employee { Id = Guid.NewGuid() };
        order.Creator = creator;

        var command = new UpdateDescriptionCommand(order, creator);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldBeValidWhenCurrentUserIsAssignee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var assignee = new Employee { Id = Guid.NewGuid() };
        order.Assignee = assignee;

        var command = new UpdateDescriptionCommand(order, assignee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldNotChangeStatusOnExecute()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee { Id = Guid.NewGuid() };
        order.Assignee = employee;

        var command = new UpdateDescriptionCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.InProgress));
    }

    [Test]
    public void ShouldReturnCurrentStatusAsBeginAndEndStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();

        var command = new UpdateDescriptionCommand(order, employee);

        Assert.That(command.GetBeginStatus(), Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(command.GetEndStatus(), Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void ShouldHaveCorrectTransitionVerbs()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new UpdateDescriptionCommand(order, employee);

        Assert.That(command.TransitionVerbPresentTense, Is.EqualTo("Save"));
        Assert.That(command.TransitionVerbPastTense, Is.EqualTo("Saved"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new UpdateDescriptionCommand(order, employee);
    }
}
