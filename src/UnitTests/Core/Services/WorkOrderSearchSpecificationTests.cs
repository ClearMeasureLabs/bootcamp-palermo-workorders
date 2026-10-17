using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void Constructor_WhenCreated_LeavesFiltersUnset()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.Status.ShouldBeNull();
        specification.Assignee.ShouldBeNull();
        specification.Creator.ShouldBeNull();
    }

    [Test]
    public void MatchStatus_WhenStatusProvided_SetsStatus()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.MatchStatus(WorkOrderStatus.Assigned);

        specification.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void MatchStatus_WhenStatusNull_ClearsStatus()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchStatus(WorkOrderStatus.Draft);

        specification.MatchStatus(null);

        specification.Status.ShouldBeNull();
    }

    [Test]
    public void MatchAssignee_WhenAssigneeProvided_SetsAssignee()
    {
        var specification = new WorkOrderSearchSpecification();
        var assignee = new Employee { UserName = "assignee" };

        specification.MatchAssignee(assignee);

        specification.Assignee.ShouldBeSameAs(assignee);
    }

    [Test]
    public void MatchAssignee_WhenAssigneeNull_ClearsAssignee()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchAssignee(new Employee { UserName = "assignee" });

        specification.MatchAssignee(null);

        specification.Assignee.ShouldBeNull();
    }

    [Test]
    public void MatchCreator_WhenCreatorProvided_SetsCreator()
    {
        var specification = new WorkOrderSearchSpecification();
        var creator = new Employee { UserName = "creator" };

        specification.MatchCreator(creator);

        specification.Creator.ShouldBeSameAs(creator);
    }

    [Test]
    public void MatchCreator_WhenCreatorNull_ClearsCreator()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchCreator(new Employee { UserName = "creator" });

        specification.MatchCreator(null);

        specification.Creator.ShouldBeNull();
    }

    [Test]
    public void MatchMethods_WhenCalledIndependently_LeaveOtherFiltersUnset()
    {
        var specification = new WorkOrderSearchSpecification();
        var assignee = new Employee { UserName = "assignee" };

        specification.MatchStatus(WorkOrderStatus.InProgress);
        specification.MatchAssignee(assignee);

        specification.Status.ShouldBe(WorkOrderStatus.InProgress);
        specification.Assignee.ShouldBeSameAs(assignee);
        specification.Creator.ShouldBeNull();
    }
}
