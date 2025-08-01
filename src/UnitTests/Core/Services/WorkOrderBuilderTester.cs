using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderBuilderTester
{
    [Test]
    public void ShouldCorrectlyBuildAWorkOrder()
    {
        var generator = new WorkOrderNumberGeneratorStub("124");

        var builder = new WorkOrderBuilder(generator);
        var creator = new Employee();
        var workOrder = builder.CreateNewWorkOrder(creator);

        Assert.That(workOrder.Creator, Is.EqualTo(creator));
        Assert.That(workOrder.Number, Is.EqualTo("124"));
        Assert.That(workOrder.Assignee, Is.Null);
        Assert.That(workOrder.Title, Is.Empty);
        Assert.That(workOrder.Description, Is.Empty);
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(workOrder.RoomNumber, Is.Null);
    }
}

public class WorkOrderNumberGeneratorStub : IWorkOrderNumberGenerator
{
    private readonly string _numberToReturn;

    public WorkOrderNumberGeneratorStub(string numberToReturn)
    {
        _numberToReturn = numberToReturn;
    }

    public string GenerateNumber()
    {
        return _numberToReturn;
    }
}