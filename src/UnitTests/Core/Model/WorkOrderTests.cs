using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void PropertiesShouldInitializeToProperDefaults()
    {
        var workOrder = new WorkOrder();
        Assert.That(workOrder.Id, Is.EqualTo(Guid.Empty));
        Assert.That(workOrder.Title, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Description, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(workOrder.Number, Is.EqualTo(null));
        Assert.That(workOrder.Creator, Is.EqualTo(null));
        Assert.That(workOrder.Assignee, Is.EqualTo(null));
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkOrder();
        order.Number = "456";
        Assert.That(order.ToString(), Is.EqualTo("Work Order 456"));
    }

    [Test]
    public void PropertiesShouldGetAndSetValuesProperly()
    {
        var workOrder = new WorkOrder();
        var guid = Guid.NewGuid();
        var creator = new Employee();
        var assignee = new Employee();
        var createdDate = new DateTime(2000, 1, 1);
        var completedDate = new DateTime(2000, 10, 1);
        var auditDate = new DateTime(2000, 1, 1, 8, 0, 0);

        workOrder.Id = guid;
        workOrder.Title = "Title";
        workOrder.Description = "Description";
        workOrder.Status = WorkOrderStatus.Complete;
        workOrder.Number = "Number";
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;

        Assert.That(workOrder.Id, Is.EqualTo(guid));
        Assert.That(workOrder.Title, Is.EqualTo("Title"));
        Assert.That(workOrder.Description, Is.EqualTo("Description"));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(workOrder.Number, Is.EqualTo("Number"));
        Assert.That(workOrder.Creator, Is.EqualTo(creator));
        Assert.That(workOrder.Assignee, Is.EqualTo(assignee));
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workOrder = new WorkOrder();
        workOrder.Status = WorkOrderStatus.Assigned;

        Assert.That(workOrder.FriendlyStatus, Is.EqualTo("Assigned"));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder();
        order.Description = longText;
        Assert.That(order.Description.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        order.ChangeStatus(WorkOrderStatus.Assigned);
        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void CanReassign_WithCurrentUser_ShouldReturnTrueForDraftStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();

        Assert.That(order.CanReassign(employee), Is.True);
    }

    [Test]
    public void CanReassign_WithCurrentUser_ShouldReturnTrueForCompleteStatusWhenUserIsCreator()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var creator = new Employee();
        order.Creator = creator;

        Assert.That(order.CanReassign(creator), Is.True);
    }

    [Test]
    public void CanReassign_WithCurrentUser_ShouldReturnFalseForCompleteStatusWhenUserIsNotCreator()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var creator = new Employee();
        var otherEmployee = new Employee();
        order.Creator = creator;

        Assert.That(order.CanReassign(otherEmployee), Is.False);
    }

    [Test]
    public void CanReassign_WithCurrentUser_ShouldReturnFalseForAssignedStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Creator = employee;

        Assert.That(order.CanReassign(employee), Is.False);
    }

    [Test]
    public void CanReassign_WithCurrentUser_ShouldReturnFalseForInProgressStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Creator = employee;

        Assert.That(order.CanReassign(employee), Is.False);
    }
}