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
    public void ShouldLookUpStatusFromCode()
    {
        Assert.That(WorkOrderStatus.FromCode("DFT"), Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(WorkOrderStatus.FromCode("ASD"), Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(WorkOrderStatus.FromCode("IPG"), Is.EqualTo(WorkOrderStatus.InProgress));
        Assert.That(WorkOrderStatus.FromCode("CMP"), Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(WorkOrderStatus.FromCode("CNL"), Is.EqualTo(WorkOrderStatus.Cancelled));
    }

    [Test]
    public void ShouldIdentifyEmptyStatus()
    {
        Assert.That(WorkOrderStatus.None.IsEmpty(), Is.True);
        Assert.That(WorkOrderStatus.Draft.IsEmpty(), Is.False);
    }

    [Test]
    public void ShouldThrowOnNullKey()
    {
        Assert.Throws<NotSupportedException>(() => WorkOrderStatus.FromKey(null));
    }

    [Test]
    public void ShouldThrowOnInvalidKey()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkOrderStatus.FromKey("nonexistent"));
    }

    [Test]
    public void ShouldReturnFriendlyNameFromToString()
    {
        Assert.That(WorkOrderStatus.Draft.ToString(), Is.EqualTo("Draft"));
        Assert.That(WorkOrderStatus.InProgress.ToString(), Is.EqualTo("In Progress"));
    }

    [Test]
    public void ShouldNotBeEqualToNonWorkOrderStatusObject()
    {
        Assert.That(WorkOrderStatus.Draft.Equals("Draft"), Is.False);
    }

    [Test]
    public void ShouldHaveConsistentHashCodes()
    {
        Assert.That(WorkOrderStatus.Draft.GetHashCode(), Is.EqualTo(WorkOrderStatus.Draft.GetHashCode()));
        Assert.That(WorkOrderStatus.Draft.GetHashCode(), Is.Not.EqualTo(WorkOrderStatus.Complete.GetHashCode()));
    }

    [Test]
    public void ShouldHaveCorrectSortByValues()
    {
        Assert.That(WorkOrderStatus.Draft.SortBy, Is.LessThan(WorkOrderStatus.Assigned.SortBy));
        Assert.That(WorkOrderStatus.Assigned.SortBy, Is.LessThan(WorkOrderStatus.InProgress.SortBy));
        Assert.That(WorkOrderStatus.InProgress.SortBy, Is.LessThan(WorkOrderStatus.Complete.SortBy));
        Assert.That(WorkOrderStatus.Complete.SortBy, Is.LessThan(WorkOrderStatus.Cancelled.SortBy));
    }
}