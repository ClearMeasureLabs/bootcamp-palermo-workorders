using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AssignedToCancelledCommandTests
{
	[Test]
	public void ShouldHaveCorrectBeginAndEndStatuses()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
		var employee = new Employee();
		workOrder.Creator = employee;

		var command = new AssignedToCancelledCommand(workOrder, employee);

		Assert.That(command.GetBeginStatus(), Is.EqualTo(WorkOrderStatus.Assigned));
		Assert.That(command.GetEndStatus(), Is.EqualTo(WorkOrderStatus.Cancelled));
	}

	[Test]
	public void ShouldBeValidWhenUserIsCreator()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
		var employee = new Employee();
		workOrder.Creator = employee;

		var command = new AssignedToCancelledCommand(workOrder, employee);

		Assert.That(command.IsValid(), Is.True);
	}

	[Test]
	public void ShouldNotBeValidWhenUserIsNotCreator()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
		var creator = new Employee();
		var otherEmployee = new Employee();
		workOrder.Creator = creator;

		var command = new AssignedToCancelledCommand(workOrder, otherEmployee);

		Assert.That(command.IsValid(), Is.False);
	}

	[Test]
	public void ShouldNotBeValidWhenStatusIsNotAssigned()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Draft };
		var employee = new Employee();
		workOrder.Creator = employee;

		var command = new AssignedToCancelledCommand(workOrder, employee);

		Assert.That(command.IsValid(), Is.False);
	}

	[Test]
	public void ShouldChangeStatusToCancelledWhenExecuted()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
		var employee = new Employee();
		workOrder.Creator = employee;

		var command = new AssignedToCancelledCommand(workOrder, employee);
		var context = new StateCommandContext { CurrentDateTime = DateTime.Now };

		command.Execute(context);

		Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Cancelled));
	}

	[Test]
	public void ShouldRecordAuditEntryWhenExecuted()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned, Number = "WO-123" };
		var employee = new Employee { UserName = "jdoe" };
		workOrder.Creator = employee;

		var command = new AssignedToCancelledCommand(workOrder, employee);
		var context = new StateCommandContext { CurrentDateTime = DateTime.Now };

		command.Execute(context);

		Assert.That(context.AuditEntries.Count, Is.EqualTo(1));
		Assert.That(context.AuditEntries[0].Action, Is.EqualTo("Cancelled"));
		Assert.That(context.AuditEntries[0].OldStatus, Is.EqualTo("Assigned"));
		Assert.That(context.AuditEntries[0].NewStatus, Is.EqualTo("Cancelled"));
		Assert.That(context.AuditEntries[0].UserName, Is.EqualTo("jdoe"));
	}
}
