using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkRequestStatusTests
{
    [Test]
    public void ShouldListAllStatuses()
    {
        var statuses = WorkRequestStatus.GetAllItems();

        Assert.That(statuses.Length, Is.EqualTo(5));
        Assert.That(statuses[0], Is.EqualTo(WorkRequestStatus.Draft));
        Assert.That(statuses[1], Is.EqualTo(WorkRequestStatus.Assigned));
        Assert.That(statuses[2], Is.EqualTo(WorkRequestStatus.InProgress));
        Assert.That(statuses[3], Is.EqualTo(WorkRequestStatus.Complete));
        Assert.That(statuses[4], Is.EqualTo(WorkRequestStatus.Cancelled));
    }

    [Test]
    public void CanParseOnKey()
    {
        var draft = WorkRequestStatus.Parse("draft");
        Assert.That(draft, Is.EqualTo(WorkRequestStatus.Draft));

        var assigned = WorkRequestStatus.Parse("assigned");
        Assert.That(assigned, Is.EqualTo(WorkRequestStatus.Assigned));

        var inprogress = WorkRequestStatus.Parse("inprogress");
        Assert.That(inprogress, Is.EqualTo(WorkRequestStatus.InProgress));

        var complete = WorkRequestStatus.Parse("complete");
        Assert.That(complete, Is.EqualTo(WorkRequestStatus.Complete));
    }

    [Test]
    public void ShouldBeRemotable()
    {
        RemotableRequestTests.AssertRemotable(WorkRequestStatus.Draft);
    }

    [Test]
    public void ShouldSerializeAndDeserializeWithJsonUsingKey()
    {
        var original = WorkRequestStatus.Complete;
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkRequestStatus>(json);

        Assert.That(deserialized, Is.EqualTo(original));
        Assert.That(json, Does.Contain(original.Key));
    }

    [Test]
    public void WorkRequestShouldSerializeCorrectly()
    {
        var workRequest = new WorkRequest
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = "Test Description",
            Status = WorkRequestStatus.Complete,
            Number = "123"
        };

        var json = JsonSerializer.Serialize(workRequest);
        var deserialized = JsonSerializer.Deserialize<WorkRequest>(json);

        Assert.That(deserialized!.Status, Is.EqualTo(workRequest.Status));
    }
}