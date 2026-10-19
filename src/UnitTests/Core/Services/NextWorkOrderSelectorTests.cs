using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class NextWorkOrderSelectorTests
{
    [Test]
    public void SelectNext_WhenNoMatchingOrders_ReturnsNull()
    {
        var technician = CreateEmployee("tech1");
        var other = CreateEmployee("other");
        var order = CreateWorkOrder("WO-0001", other, WorkOrderStatus.Assigned, new DateTime(2024, 1, 1));

        var result = NextWorkOrderSelector.SelectNext([order], technician.UserName);

        result.ShouldBeNull();
    }

    [Test]
    public void SelectNext_WhenMultipleAssigned_ReturnsOldestByAssignedDate()
    {
        var technician = CreateEmployee("tech1");
        var older = CreateWorkOrder("WO-0001", technician, WorkOrderStatus.Assigned, new DateTime(2024, 1, 1));
        var newer = CreateWorkOrder("WO-0002", technician, WorkOrderStatus.Assigned, new DateTime(2024, 6, 1));

        var result = NextWorkOrderSelector.SelectNext([newer, older], technician.UserName);

        result.ShouldBe(older);
    }

    [Test]
    public void SelectNext_WhenOnlyDraft_ReturnsNull()
    {
        var technician = CreateEmployee("tech1");
        var order = CreateWorkOrder("WO-0001", technician, WorkOrderStatus.Draft, null);

        var result = NextWorkOrderSelector.SelectNext([order], technician.UserName);

        result.ShouldBeNull();
    }

    [Test]
    public void SelectNext_WhenInProgress_IncludesOrder()
    {
        var technician = CreateEmployee("tech1");
        var order = CreateWorkOrder("WO-0001", technician, WorkOrderStatus.InProgress, new DateTime(2024, 3, 1));

        var result = NextWorkOrderSelector.SelectNext([order], technician.UserName);

        result.ShouldBe(order);
    }

    private static Employee CreateEmployee(string userName) =>
        new()
        {
            UserName = userName,
            FirstName = "Test",
            LastName = "User",
            EmailAddress = $"{userName}@example.com"
        };

    private static WorkOrder CreateWorkOrder(
        string number,
        Employee assignee,
        WorkOrderStatus status,
        DateTime? assignedDate)
    {
        var order = new WorkOrder
        {
            Number = number,
            Title = "Title",
            Description = "Description",
            Creator = assignee,
            Assignee = assignee,
            AssignedDate = assignedDate
        };
        if (status != WorkOrderStatus.Draft)
        {
            order.ChangeStatus(status);
        }

        return order;
    }
}
