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
    public void ShouldConsiderStatusesFromDifferentFactoryMethodsEqualByValue()
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
        WorkOrderStatus? draft = NullableDraft();

        (left == right).ShouldBeTrue();
        (draft == null).ShouldBeFalse();
        (null == draft).ShouldBeFalse();
    }

    private static WorkOrderStatus? NullableDraft() => WorkOrderStatus.Draft;

    [Test]
    public void WhenComparingRoundTrippedJsonStatusShouldReturnCanonicalInstanceEqualByValue()
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

    [Test]
    public void ShouldNotThrowWhenComparingAnInstanceWithNullCodeFromTheParameterlessConstructor()
    {
        var uninitialized = new WorkOrderStatus();

        Should.NotThrow(() => _ = uninitialized == WorkOrderStatus.Draft);
        Should.NotThrow(() => _ = WorkOrderStatus.Draft == uninitialized);
        (uninitialized == WorkOrderStatus.Draft).ShouldBeFalse();
        (uninitialized != WorkOrderStatus.Draft).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotConsiderTwoSeparateUninitializedInstancesEqual()
    {
        var first = new WorkOrderStatus();
        var second = new WorkOrderStatus();

        ReferenceEquals(first, second).ShouldBeFalse();
        Should.NotThrow(() => _ = first == second);
        (first == second).ShouldBeFalse();
        (first != second).ShouldBeTrue();
    }

    [Test]
    public void ShouldConsiderAnUninitializedInstanceEqualToItselfByReference()
    {
        var uninitialized = new WorkOrderStatus();
        var sameInstance = uninitialized;

#pragma warning disable CS1718 // Comparison made to same variable is intentional here
        (uninitialized == sameInstance).ShouldBeTrue();
        (uninitialized != sameInstance).ShouldBeFalse();
#pragma warning restore CS1718
    }

    [Test]
    public void ShouldNotThrowWhenCallingEqualsOnAnInstanceWithNullCode()
    {
        var uninitialized = new WorkOrderStatus();

        Should.NotThrow(() => uninitialized.Equals(WorkOrderStatus.Draft));
        Should.NotThrow(() => WorkOrderStatus.Draft.Equals(uninitialized));
        uninitialized.Equals(WorkOrderStatus.Draft).ShouldBeFalse();
        WorkOrderStatus.Draft.Equals(uninitialized).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotThrowWhenCallingGetHashCodeOnAnInstanceWithNullCode()
    {
        var uninitialized = new WorkOrderStatus();

        Should.NotThrow(() => uninitialized.GetHashCode());
    }

    [Test]
    public void WhenComparingTwoUninitializedInstancesEqualsShouldAgreeWithOperator()
    {
        var first = new WorkOrderStatus();
        var second = new WorkOrderStatus();

        (first == second).ShouldBeFalse();
        first.Equals(second).ShouldBeFalse();
        (first == second).ShouldBe(first.Equals(second));
    }

    [Test]
    public void ShouldAgreeBetweenEqualsOperatorForEveryCombinationOfNullAndNonNullCode()
    {
        var uninitialized = new WorkOrderStatus();
        var separateInstance = CreateSeparateInstanceWithSameCodeAs(WorkOrderStatus.Draft);

        (WorkOrderStatus.Draft == separateInstance).ShouldBe(WorkOrderStatus.Draft.Equals(separateInstance));
        (WorkOrderStatus.Draft == uninitialized).ShouldBe(WorkOrderStatus.Draft.Equals(uninitialized));
        (uninitialized == WorkOrderStatus.Draft).ShouldBe(uninitialized.Equals(WorkOrderStatus.Draft));
        var same = uninitialized;
        same.Equals(uninitialized).ShouldBeTrue();
        ReferenceEquals(same, uninitialized).ShouldBeTrue();
    }

    [Test]
    public void EqualStatusesShouldProduceEqualHashCodes()
    {
        var separateInstance = CreateSeparateInstanceWithSameCodeAs(WorkOrderStatus.Draft);

        WorkOrderStatus.Draft.Equals(separateInstance).ShouldBeTrue();
        WorkOrderStatus.Draft.GetHashCode().ShouldBe(separateInstance.GetHashCode());
    }

    [Test]
    public void ShouldNotThrowWhenUsingAnInstanceWithNullCodeAsADictionaryKey()
    {
        var uninitialized = new WorkOrderStatus();

        Should.NotThrow(() =>
        {
            var dictionary = new Dictionary<WorkOrderStatus, string> { { uninitialized, "value" } };
            dictionary.ContainsKey(uninitialized).ShouldBeTrue();
        });
    }

    [Test]
    public void ShouldNotThrowWhenCallingIsEmptyOnAnInstanceWithNullCode()
    {
        var uninitialized = new WorkOrderStatus();

        Should.NotThrow(() => uninitialized.IsEmpty());
        uninitialized.IsEmpty().ShouldBeFalse();
    }

    [Test]
    public void Equals_ShouldReturnFalse_WhenOtherIsNull()
    {
        WorkOrderStatus.Draft.Equals(null).ShouldBeFalse();
    }

    [Test]
    public void Equals_ShouldReturnFalse_WhenOtherIsDifferentType()
    {
        WorkOrderStatus.Draft.Equals(new object()).ShouldBeFalse();
    }

    [Test]
    public void Equals_ShouldReturnTrue_WhenCodesMatch()
    {
        WorkOrderStatus.Draft.Equals(WorkOrderStatus.Draft).ShouldBeTrue();
        WorkOrderStatus.Draft.GetHashCode().ShouldBe(WorkOrderStatus.FromCode("DRT").GetHashCode());
    }

    [Test]
    public void Equals_ShouldReturnFalse_WhenCodesDiffer()
    {
        WorkOrderStatus.Draft.Equals(WorkOrderStatus.Assigned).ShouldBeFalse();
    }

    [Test]
    public void FromCode_ShouldReturnMatchingStatus()
    {
        WorkOrderStatus.FromCode("ASD").ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void FromKey_ShouldThrow_WhenKeyIsNull()
    {
        Should.Throw<ArgumentNullException>(() => WorkOrderStatus.FromKey(null));
    }

    [Test]
    public void IsEmpty_ShouldBeTrue_ForNone()
    {
        WorkOrderStatus.None.IsEmpty().ShouldBeTrue();
        WorkOrderStatus.Draft.IsEmpty().ShouldBeFalse();
    }

    [Test]
    public void ToString_ShouldReturnFriendlyName()
    {
        WorkOrderStatus.InProgress.ToString().ShouldBe("In Progress");
    }
}