using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void ShouldDefaultToNullProperties()
    {
        var spec = new WorkOrderSearchSpecification();

        spec.Status.ShouldBeNull();
        spec.Assignee.ShouldBeNull();
        spec.Creator.ShouldBeNull();
    }

    [Test]
    public void ShouldMatchStatus()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchStatus(WorkOrderStatus.InProgress);

        spec.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public void ShouldMatchAssignee()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee("jdoe", "John", "Doe", "jdoe@test.com");
        spec.MatchAssignee(employee);

        spec.Assignee.ShouldBe(employee);
    }

    [Test]
    public void ShouldMatchCreator()
    {
        var spec = new WorkOrderSearchSpecification();
        var employee = new Employee("jdoe", "John", "Doe", "jdoe@test.com");
        spec.MatchCreator(employee);

        spec.Creator.ShouldBe(employee);
    }

    [Test]
    public void ShouldAllowNullValues()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchStatus(WorkOrderStatus.Draft);
        spec.MatchAssignee(new Employee());
        spec.MatchCreator(new Employee());

        spec.MatchStatus(null);
        spec.MatchAssignee(null);
        spec.MatchCreator(null);

        spec.Status.ShouldBeNull();
        spec.Assignee.ShouldBeNull();
        spec.Creator.ShouldBeNull();
    }
}
