using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class ReassignWorkOrderCommandTests
{
    [Test]
    public void ReassignWorkOrderCommand_ShouldChangeAssignee()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        creator.AddRole(new Role("admin", true, true));
        var originalAssignee = new Employee("original", "Bob", "Jones", "bob@test.com");
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = creator, Assignee = originalAssignee };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);
        command.Execute();

        order.Assignee.ShouldBe(newAssignee);
    }

    [Test]
    public void ReassignWorkOrderCommand_ShouldResetStatusToAssigned_WhenInProgress()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        creator.AddRole(new Role("admin", true, true));
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);
        command.Execute();

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void ReassignWorkOrderCommand_ShouldMaintainAssignedStatus_WhenAlreadyAssigned()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        creator.AddRole(new Role("admin", true, true));
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);
        command.Execute();

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void ReassignWorkOrderCommand_ShouldRejectNonCreatorRequester()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        creator.Id = Guid.NewGuid();
        var nonCreator = new Employee("other", "Dave", "Brown", "dave@test.com");
        nonCreator.Id = Guid.NewGuid();
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, nonCreator);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ReassignWorkOrderCommand_ShouldRejectAssigneeWithoutFulfillRole()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        // newAssignee has no roles — cannot fulfill

        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void IsValid_ShouldReturnTrue_WhenAssignedStatusAndCreatorAndAssigneeHasFulfillRole()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void IsValid_ShouldReturnTrue_WhenInProgressStatusAndCreatorAndAssigneeHasFulfillRole()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void IsValid_ShouldReturnFalse_WhenDraftStatus()
    {
        var creator = new Employee("creator", "Alice", "Smith", "alice@test.com");
        var newAssignee = new Employee("new", "Carol", "White", "carol@test.com");
        newAssignee.AddRole(new Role("worker", false, true));

        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = creator };

        var command = new ReassignWorkOrderCommand(order, newAssignee, creator);

        command.IsValid().ShouldBeFalse();
    }
}
