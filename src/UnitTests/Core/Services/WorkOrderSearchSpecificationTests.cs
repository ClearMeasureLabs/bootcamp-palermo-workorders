using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void NewSpecification_Should_HaveAllMatchersNull()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.Status.ShouldBeNull();
        specification.Assignee.ShouldBeNull();
        specification.Creator.ShouldBeNull();
    }

    [Test]
    public void MatchStatus_Should_SetStatus_And_AcceptNull()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.Status.ShouldBe(WorkOrderStatus.Assigned);

        specification.MatchStatus(null);
        specification.Status.ShouldBeNull();
    }

    [Test]
    public void MatchAssignee_Should_SetAssignee_And_AcceptNull()
    {
        var specification = new WorkOrderSearchSpecification();
        var assignee = new Employee();

        specification.MatchAssignee(assignee);
        specification.Assignee.ShouldBe(assignee);

        specification.MatchAssignee(null);
        specification.Assignee.ShouldBeNull();
    }

    [Test]
    public void MatchCreator_Should_SetCreator_And_AcceptNull()
    {
        var specification = new WorkOrderSearchSpecification();
        var creator = new Employee();

        specification.MatchCreator(creator);
        specification.Creator.ShouldBe(creator);

        specification.MatchCreator(null);
        specification.Creator.ShouldBeNull();
    }
}
