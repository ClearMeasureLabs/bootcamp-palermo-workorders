using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderStatusEqualityTests
{
    [Test]
    public void Equals_SameCodeDifferentInstance_ShouldReturnTrue()
    {
        var first = WorkOrderStatus.FromCode("DRT");
        var second = WorkOrderStatus.FromCode("DRT");

        var result = first.Equals(second);

        result.ShouldBeTrue();
    }

    [Test]
    public void Equals_DifferentCode_ShouldReturnFalse()
    {
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;

        var result = draft.Equals(assigned);

        result.ShouldBeFalse();
    }

    [Test]
    public void Equals_Null_ShouldReturnFalse()
    {
        var draft = WorkOrderStatus.Draft;

        var result = draft.Equals(null);

        result.ShouldBeFalse();
    }

    [Test]
    public void GetHashCode_EqualInstances_ShouldMatch()
    {
        var first = WorkOrderStatus.FromKey("draft");
        var second = WorkOrderStatus.FromKey("Draft");

        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Test]
    public void EqualityOperator_SameCodeDifferentInstance_ShouldReturnTrue()
    {
        var first = WorkOrderStatus.FromCode("ASD");
        var second = WorkOrderStatus.FromCode("ASD");

        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
    }

    [Test]
    public void EqualityOperator_DifferentStatus_ShouldReturnFalse()
    {
        var inProgress = WorkOrderStatus.InProgress;
        var complete = WorkOrderStatus.Complete;

        (inProgress == complete).ShouldBeFalse();
        (inProgress != complete).ShouldBeTrue();
    }

    [Test]
    public void EqualityOperator_BothNull_ShouldReturnTrue()
    {
        WorkOrderStatus? left = null;
        WorkOrderStatus? right = null;

        (left == right).ShouldBeTrue();
    }

    [Test]
    public void EqualityOperator_LeftNull_ShouldReturnFalse()
    {
        WorkOrderStatus? left = null;
        var right = WorkOrderStatus.Draft;

        (left == right).ShouldBeFalse();
        (right == left).ShouldBeFalse();
    }

    [Test]
    public void WhenUsedAsDictionaryKey_ShouldFindValueByEqualInstance()
    {
        var dictionary = new Dictionary<WorkOrderStatus, string>
        {
            [WorkOrderStatus.Draft] = "draft-value"
        };

        var lookupKey = WorkOrderStatus.FromCode("DRT");

        dictionary.TryGetValue(lookupKey, out var value).ShouldBeTrue();
        value.ShouldBe("draft-value");
    }

    [Test]
    public void WhenUsedAsHashSetMember_ShouldTreatEqualInstancesAsDuplicates()
    {
        var set = new HashSet<WorkOrderStatus>
        {
            WorkOrderStatus.Complete,
            WorkOrderStatus.FromCode("CMP")
        };

        set.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldMatchWorkOrderStatusReturnedFromChangeStatus()
    {
        var workOrder = new WorkOrder
        {
            Status = WorkOrderStatus.Draft
        };

        workOrder.ChangeStatus(WorkOrderStatus.FromKey("assigned"));

        (workOrder.Status == WorkOrderStatus.Assigned).ShouldBeTrue();
    }
}
