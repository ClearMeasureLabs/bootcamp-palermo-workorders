using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void CanReassign_WhenDraft_ReturnsTrue()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        workOrder.Status = WorkOrderStatus.Draft;

        workOrder.CanReassign().ShouldBeTrue();
    }

    [TestCase(nameof(WorkOrderStatus.Assigned))]
    [TestCase(nameof(WorkOrderStatus.InProgress))]
    [TestCase(nameof(WorkOrderStatus.Complete))]
    [TestCase(nameof(WorkOrderStatus.Cancelled))]
    public void CanReassign_WhenNotDraft_ReturnsFalse(string statusKey)
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        workOrder.Status = WorkOrderStatus.FromKey(statusKey);

        workOrder.CanReassign().ShouldBeFalse();
    }

    [Test]
    public void ToString_ReturnsWorkOrderNumberText()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        workOrder.Number = "WO-123";

        workOrder.ToString().ShouldBe("Work Order WO-123");
    }

    [Test]
    public void GetMessage_ReturnsWorkOrderStatusText()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        workOrder.Number = "WO-123";
        workOrder.Status = WorkOrderStatus.Assigned;

        workOrder.GetMessage().ShouldBe("Work Order WO-123 is now in Status Assigned");
    }

    [Test]
    public void Description_WhenNull_BecomesEmpty()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();

        workOrder.Description = null;

        workOrder.Description.ShouldBe(string.Empty);
    }

    [Test]
    public void Description_WhenLongerThanMax_IsTruncated()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        var value = new string('a', WorkOrder.DescriptionMaxLength + 10);

        workOrder.Description = value;

        workOrder.Description.ShouldBe(value[..WorkOrder.DescriptionMaxLength]);
    }

    [Test]
    public void Instructions_WhenNull_BecomesEmpty()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();

        workOrder.Instructions = null;

        workOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public void Instructions_WhenLongerThanMax_IsTruncated()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        var value = new string('b', WorkOrder.InstructionsMaxLength + 10);

        workOrder.Instructions = value;

        workOrder.Instructions.ShouldBe(value[..WorkOrder.InstructionsMaxLength]);
    }

    [Test]
    public void ChangeStatus_WithEmployeeAndDate_SetsStatus()
    {
        var workOrder = ObjectMother.Faker<WorkOrder>();
        workOrder.Status = WorkOrderStatus.Draft;
        var employee = ObjectMother.Faker<Employee>();
        var date = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        workOrder.ChangeStatus(employee, date, WorkOrderStatus.InProgress);

        workOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
    }
}
