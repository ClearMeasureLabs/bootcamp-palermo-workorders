using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class EntityBaseTests
{
    [Test]
    public void ShouldBeEqual_WhenSameNonEmptyIdAndSameType()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity { Id = id };
        var right = new TestEntity { Id = id };

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Test]
    public void ShouldNotBeEqual_WhenIdsDiffer()
    {
        var left = new TestEntity { Id = Guid.NewGuid() };
        var right = new TestEntity { Id = Guid.NewGuid() };

        left.Equals(right).ShouldBeFalse();
        (left == right).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqual_WhenIdIsEmpty()
    {
        var left = new TestEntity { Id = Guid.Empty };
        var right = new TestEntity { Id = Guid.Empty };

        left.Equals(right).ShouldBeFalse();
        (left == right).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqual_WhenOtherIsNull()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        TestEntity? other = null;

        entity.Equals(other).ShouldBeFalse();
        (entity == other).ShouldBeFalse();
        (entity != other).ShouldBeTrue();
        (other != entity).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeEqual_WhenComparedToDifferentType()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var other = new OtherTestEntity { Id = entity.Id };

        // Intentional cross-type Equals contract check (same Id, different EntityBase<T>).
        // ReSharper disable once SuspiciousTypeConversion.Global
        object otherAsObject = other;
        entity.Equals(otherAsObject).ShouldBeFalse();
    }

    [Test]
    public void ShouldBeEqual_WhenSameReference()
    {
        var entity = new TestEntity { Id = Guid.NewGuid() };

        entity.Equals(entity).ShouldBeTrue();
        entity.Equals((object)entity).ShouldBeTrue();
    }

    [Test]
    public void ShouldIncludeIdInToString()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity { Id = id };

        entity.ToString().ShouldContain(id.ToString());
    }

    private sealed class TestEntity : EntityBase<TestEntity>
    {
        public override Guid Id { get; set; }
    }

    private sealed class OtherTestEntity : EntityBase<OtherTestEntity>
    {
        public override Guid Id { get; set; }
    }
}
