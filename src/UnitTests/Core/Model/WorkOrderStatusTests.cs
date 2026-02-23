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

        Assert.That(statuses.Length, Is.EqualTo(5));
        Assert.That(statuses[0], Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(statuses[1], Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(statuses[2], Is.EqualTo(WorkOrderStatus.InProgress));
        Assert.That(statuses[3], Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(statuses[4], Is.EqualTo(WorkOrderStatus.Cancelled));
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

    [Test]
    public void ShouldLookUpFromCode()
    {
        var result = WorkOrderStatus.FromCode("DFT");
        Assert.That(result, Is.EqualTo(WorkOrderStatus.Draft));

        var assigned = WorkOrderStatus.FromCode("ASD");
        Assert.That(assigned, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void ShouldThrowOnNullKey()
    {
        Assert.Throws<NotSupportedException>(() => WorkOrderStatus.FromKey(null));
    }

    [Test]
    public void ShouldThrowOnInvalidKey()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkOrderStatus.FromKey("InvalidKey"));
    }

    [Test]
    public void NoneShouldBeEmpty()
    {
        Assert.That(WorkOrderStatus.None.IsEmpty(), Is.True);
    }

    [Test]
    public void DraftShouldNotBeEmpty()
    {
        Assert.That(WorkOrderStatus.Draft.IsEmpty(), Is.False);
    }

    [Test]
    public void ShouldReturnFriendlyNameFromToString()
    {
        Assert.That(WorkOrderStatus.InProgress.ToString(), Is.EqualTo("In Progress"));
        Assert.That(WorkOrderStatus.Draft.ToString(), Is.EqualTo("Draft"));
    }

    [Test]
    public void ShouldNotBeEqualToNonWorkOrderStatusObject()
    {
        Assert.That(WorkOrderStatus.Draft.Equals("not a status"), Is.False);
    }

    [Test]
    public void ShouldNotBeEqualToNull()
    {
        Assert.That(WorkOrderStatus.Draft.Equals(null), Is.False);
    }

    [Test]
    public void ShouldHaveConsistentHashCode()
    {
        Assert.That(WorkOrderStatus.Draft.GetHashCode(), Is.EqualTo(WorkOrderStatus.Draft.GetHashCode()));
    }
}