using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

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
    public void WorkOrder_SlaResponseHours_ShouldDefaultToNull()
    {
        var workOrder = new WorkOrder();
        workOrder.SlaResponseHours.ShouldBeNull();
    }

    [Test]
    public void WorkOrder_SlaResolutionHours_ShouldDefaultToNull()
    {
        var workOrder = new WorkOrder();
        workOrder.SlaResolutionHours.ShouldBeNull();
    }

    [Test]
    public void WorkOrder_GetResponseSlaStatus_ShouldReturnNull_WhenNoSlaSet()
    {
        var workOrder = new WorkOrder();
        workOrder.GetResponseSlaStatus().ShouldBeNull();
    }

    [Test]
    public void WorkOrder_GetResponseSlaStatus_ShouldReturnOnTrack_WhenUnder75Percent()
    {
        var now = DateTime.UtcNow;
        var workOrder = new WorkOrder
        {
            SlaResponseHours = 8,
            CreatedDate = now.AddHours(-4),  // 50% elapsed
            AssignedDate = now
        };
        workOrder.GetResponseSlaStatus().ShouldBe(SlaStatus.OnTrack);
    }

    [Test]
    public void WorkOrder_GetResponseSlaStatus_ShouldReturnAtRisk_WhenBetween75And100Percent()
    {
        var now = DateTime.UtcNow;
        var workOrder = new WorkOrder
        {
            SlaResponseHours = 8,
            CreatedDate = now.AddHours(-7),  // 87.5% elapsed
            AssignedDate = now
        };
        workOrder.GetResponseSlaStatus().ShouldBe(SlaStatus.AtRisk);
    }

    [Test]
    public void WorkOrder_GetResponseSlaStatus_ShouldReturnBreached_WhenOver100Percent()
    {
        var now = DateTime.UtcNow;
        var workOrder = new WorkOrder
        {
            SlaResponseHours = 4,
            CreatedDate = now.AddHours(-5),  // 125% elapsed
            AssignedDate = now
        };
        workOrder.GetResponseSlaStatus().ShouldBe(SlaStatus.Breached);
    }

    [Test]
    public void WorkOrder_GetResolutionSlaStatus_ShouldUseCompletedDate_WhenComplete()
    {
        var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var completed = created.AddHours(20);  // 20/24 = 83% elapsed → AtRisk
        var workOrder = new WorkOrder
        {
            SlaResolutionHours = 24,
            CreatedDate = created,
            CompletedDate = completed,
            Status = WorkOrderStatus.Complete
        };
        workOrder.GetResolutionSlaStatus().ShouldBe(SlaStatus.AtRisk);
    }
}