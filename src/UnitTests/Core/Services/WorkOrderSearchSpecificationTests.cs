using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void ShouldDefaultToNullProperties()
    {
        var spec = new WorkOrderSearchSpecification();

        Assert.That(spec.Status, Is.Null);
        Assert.That(spec.Assignee, Is.Null);
        Assert.That(spec.Creator, Is.Null);
    }

    [Test]
    public void ShouldMatchStatus()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchStatus(WorkOrderStatus.Draft);

        Assert.That(spec.Status, Is.EqualTo(WorkOrderStatus.Draft));
    }

    [Test]
    public void ShouldMatchAssignee()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee("jdoe", "John", "Doe", "jdoe@test.com");
        spec.MatchAssignee(employee);

        Assert.That(spec.Assignee, Is.EqualTo(employee));
    }

    [Test]
    public void ShouldMatchCreator()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee("jdoe", "John", "Doe", "jdoe@test.com");
        spec.MatchCreator(employee);

        Assert.That(spec.Creator, Is.EqualTo(employee));
    }

    [Test]
    public void ShouldAllowNullStatus()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchStatus(WorkOrderStatus.Draft);
        spec.MatchStatus(null);

        Assert.That(spec.Status, Is.Null);
    }

    [Test]
    public void ShouldAllowNullAssignee()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee();
        spec.MatchAssignee(employee);
        spec.MatchAssignee(null);

        Assert.That(spec.Assignee, Is.Null);
    }

    [Test]
    public void ShouldAllowNullCreator()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee();
        spec.MatchCreator(employee);
        spec.MatchCreator(null);

        Assert.That(spec.Creator, Is.Null);
    }
}
