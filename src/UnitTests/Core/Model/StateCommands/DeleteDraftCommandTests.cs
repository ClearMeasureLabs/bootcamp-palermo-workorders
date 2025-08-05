using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class DeleteDraftCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidWhenNotPersistedToDB()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.None;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DeleteDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValidWhenInDraft()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DeleteDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }
    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new DeleteDraftCommand(order, employee);
    }
}