using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderStatusTests
{
    [Test]
    public void ShouldListAllStatuses()
    {
        var statuses = WorkOrderStatus.GetAllItems();

        Assert.That(statuses.Length, Is.EqualTo(6));
        Assert.That(statuses[0], Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(statuses[1], Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(statuses[2], Is.EqualTo(WorkOrderStatus.InProgress));
        Assert.That(statuses[3], Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(statuses[4], Is.EqualTo(WorkOrderStatus.Cancelled));
        Assert.That(statuses[5], Is.EqualTo(WorkOrderStatus.Deleted));
    }

    [Test]
    public void CanParseOnKey()
    {
        var draft = WorkOrderStatus.Parse("draft");
        Assert.That(draft, Is.EqualTo(WorkOrderStatus.Draft));

        var assigned = WorkOrderStatus.Parse("assigned");
        Assert.That(assigned, Is.EqualTo(WorkOrderStatus.Assigned));

        var inprogress = WorkOrderStatus.Parse("inprogress");
        Assert.That(inprogress, Is.EqualTo(WorkOrderStatus.InProgress));

        var complete = WorkOrderStatus.Parse("complete");
        Assert.That(complete, Is.EqualTo(WorkOrderStatus.Complete));
    }

    [Test]
    public void ShouldBeRemotable()
    {
        RemotableRequestTests.AssertRemotable(WorkOrderStatus.Draft);
    }

    [Test]
    public void ShouldSerializeAndDeserializeWithJsonUsingKey()
    {
        var original = WorkOrderStatus.Complete;
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkOrderStatus>(json);

        Assert.That(deserialized, Is.EqualTo(original));
        Assert.That(json, Does.Contain(original.Key));
    }

    [Test]
    public void WorkOrderShouldSerializeCorrectly()
    {
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = "Test Description",
            Status = WorkOrderStatus.Complete,
            Number = "123"
        };

        var json = JsonSerializer.Serialize(workOrder);
        var deserialized = JsonSerializer.Deserialize<WorkOrder>(json);

        Assert.That(deserialized!.Status, Is.EqualTo(workOrder.Status));
    }
}