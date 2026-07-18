using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkRequestBuilderTests
{
    [Test]
    public void ShouldCorrectlyBuildAWorkRequest()
    {
        var generator = new WorkRequestNumberGeneratorStub("124");

        var builder = new WorkRequestBuilder(generator);
        var creator = new Employee();
        var workRequest = builder.CreateNewWorkRequest(creator);

        Assert.That(workRequest.Creator, Is.EqualTo(creator));
        Assert.That(workRequest.Number, Is.EqualTo("124"));
        Assert.That(workRequest.Assignee, Is.Null);
        Assert.That(workRequest.Title, Is.Empty);
        Assert.That(workRequest.Description, Is.Empty);
        Assert.That(workRequest.Status, Is.EqualTo(WorkRequestStatus.Draft));
        Assert.That(workRequest.RoomNumber, Is.Null);
    }
}

public class WorkRequestNumberGeneratorStub : IWorkRequestNumberGenerator
{
    private readonly string _numberToReturn;

    public WorkRequestNumberGeneratorStub(string numberToReturn)
    {
        _numberToReturn = numberToReturn;
    }

    public string GenerateNumber()
    {
        return _numberToReturn;
    }
}