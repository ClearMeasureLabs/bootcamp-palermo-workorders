using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToAssignedCommandTests
{
	[Test]
	public void ShouldHaveCorrectBeginAndEndStatuses()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };
		var employee = new Employee();
		workOrder.Assignee = employee;

		var command = new InProgressToAssignedCommand(workOrder, employee);

		Assert.That(command.GetBeginStatus(), Is.EqualTo(WorkOrderStatus.InProgress));
		Assert.That(command.GetEndStatus(), Is.EqualTo(WorkOrderStatus.Assigned));
	}

	[Test]
	public void ShouldBeValidWhenUserIsAssignee()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };
		var employee = new Employee();
		workOrder.Assignee = employee;

		var command = new InProgressToAssignedCommand(workOrder, employee);

		Assert.That(command.IsValid(), Is.True);
	}

	[Test]
	public void ShouldNotBeValidWhenUserIsNotAssignee()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };
		var assignee = new Employee();
		var otherEmployee = new Employee();
		workOrder.Assignee = assignee;

		var command = new InProgressToAssignedCommand(workOrder, otherEmployee);

		Assert.That(command.IsValid(), Is.False);
	}

	[Test]
	public void ShouldNotBeValidWhenStatusIsNotInProgress()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };
		var employee = new Employee();
		workOrder.Assignee = employee;

		var command = new InProgressToAssignedCommand(workOrder, employee);

		Assert.That(command.IsValid(), Is.False);
	}

	[Test]
	public void ShouldChangeStatusToAssignedWhenExecuted()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };
		var employee = new Employee();
		workOrder.Assignee = employee;

		var command = new InProgressToAssignedCommand(workOrder, employee);
		var context = new StateCommandContext { CurrentDateTime = DateTime.Now };

		command.Execute(context);

		Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Assigned));
	}

	[Test]
	public void ShouldRecordAuditEntryWhenExecuted()
	{
		var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress, Number = "WO-123" };
		var employee = new Employee { UserName = "jdoe" };
		workOrder.Assignee = employee;

		var command = new InProgressToAssignedCommand(workOrder, employee);
		var context = new StateCommandContext { CurrentDateTime = DateTime.Now };

		command.Execute(context);

		Assert.That(context.AuditEntries.Count, Is.EqualTo(1));
		Assert.That(context.AuditEntries[0].Action, Is.EqualTo("Shelved"));
		Assert.That(context.AuditEntries[0].OldStatus, Is.EqualTo("In Progress"));
		Assert.That(context.AuditEntries[0].NewStatus, Is.EqualTo("Assigned"));
		Assert.That(context.AuditEntries[0].UserName, Is.EqualTo("jdoe"));
	}
}
