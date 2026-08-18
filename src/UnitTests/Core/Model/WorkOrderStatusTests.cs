using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Shouldly;

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
    public void ShouldConsiderTwoDistinctInstancesWithTheSameCodeEqualByValue()
    {
        var fromCode = WorkOrderStatus.FromCode("DRT");
        var fromKey = WorkOrderStatus.FromKey("draft");

        (fromCode == fromKey).ShouldBeTrue();
        (fromCode != fromKey).ShouldBeFalse();
        (WorkOrderStatus.Draft == WorkOrderStatus.FromKey("draft")).ShouldBeTrue();
    }

    [Test]
    public void ShouldConsiderDifferentStatusesUnequalByValue()
    {
        (WorkOrderStatus.Draft == WorkOrderStatus.Assigned).ShouldBeFalse();
        (WorkOrderStatus.Draft != WorkOrderStatus.Assigned).ShouldBeTrue();
    }

    [Test]
    public void ShouldTreatNullOperandsAsUnequalUnlessBothNull()
    {
        WorkOrderStatus? left = null;
        WorkOrderStatus? right = null;

        (left == right).ShouldBeTrue();
        (WorkOrderStatus.Draft == null).ShouldBeFalse();
        (null == WorkOrderStatus.Draft).ShouldBeFalse();
    }

    [Test]
    public void WhenComparingRoundTrippedJsonStatusShouldBeValueEqualNotReferenceEqual()
    {
        var original = WorkOrderStatus.Complete;
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkOrderStatus>(json);

        ReferenceEquals(deserialized, original).ShouldBeTrue();
        (deserialized == original).ShouldBeTrue();
    }

    [Test]
    public void WhenTwoSeparateInstancesShareTheSameCodeTheyShouldStillCompareEqualByValue()
    {
        var separateInstance = CreateSeparateInstanceWithSameCodeAs(WorkOrderStatus.Draft);

        ReferenceEquals(separateInstance, WorkOrderStatus.Draft).ShouldBeFalse();
        (separateInstance == WorkOrderStatus.Draft).ShouldBeTrue();
        (separateInstance != WorkOrderStatus.Draft).ShouldBeFalse();
        separateInstance.Equals(WorkOrderStatus.Draft).ShouldBeTrue();
    }

    private static WorkOrderStatus CreateSeparateInstanceWithSameCodeAs(WorkOrderStatus source)
    {
        var ctor = typeof(WorkOrderStatus).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(string), typeof(string), typeof(byte) },
            null)!;

        return (WorkOrderStatus)ctor.Invoke(new object[] { source.Code, source.Key, source.FriendlyName, source.SortBy });
    }
}