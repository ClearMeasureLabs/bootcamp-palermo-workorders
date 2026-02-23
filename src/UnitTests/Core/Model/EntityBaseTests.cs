using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class EntityBaseTests
{
    [Test]
    public void ShouldBeEqualWhenSameId()
    {
        var id = Guid.NewGuid();
        var entity1 = new Employee { Id = id };
        var entity2 = new Employee { Id = id };

        entity1.Equals(entity2).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeEqualWhenDifferentId()
    {
        var entity1 = new Employee { Id = Guid.NewGuid() };
        var entity2 = new Employee { Id = Guid.NewGuid() };

        entity1.Equals(entity2).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqualWhenBothIdsAreEmpty()
    {
        var entity1 = new Employee();
        var entity2 = new Employee();

        entity1.Equals(entity2).ShouldBeFalse();
    }

    [Test]
    public void ShouldBeEqualToSameReference()
    {
        var entity = new Employee { Id = Guid.NewGuid() };

        entity.Equals(entity).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeEqualToNull()
    {
        var entity = new Employee { Id = Guid.NewGuid() };

        entity.Equals((Employee?)null).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqualToNullObject()
    {
        var entity = new Employee { Id = Guid.NewGuid() };

        entity.Equals((object?)null).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqualToDifferentType()
    {
        var employee = new Employee { Id = Guid.NewGuid() };
        var role = new Role("test", true, false) { Id = employee.Id };

        employee.Equals((object)role).ShouldBeFalse();
    }

    [Test]
    public void ShouldSupportEqualityOperator()
    {
        var id = Guid.NewGuid();
        var entity1 = new Employee { Id = id };
        var entity2 = new Employee { Id = id };

        (entity1 == entity2).ShouldBeTrue();
    }

    [Test]
    public void ShouldSupportInequalityOperator()
    {
        var entity1 = new Employee { Id = Guid.NewGuid() };
        var entity2 = new Employee { Id = Guid.NewGuid() };

        (entity1 != entity2).ShouldBeTrue();
    }

    [Test]
    public void ShouldHandleNullInEqualityOperator()
    {
        var entity = new Employee { Id = Guid.NewGuid() };

        (entity == null).ShouldBeFalse();
        (null == entity).ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnConsistentHashCode()
    {
        var id = Guid.NewGuid();
        var entity1 = new Employee { Id = id };
        var entity2 = new Employee { Id = id };

        entity1.GetHashCode().ShouldBe(entity2.GetHashCode());
    }

    [Test]
    public void ToStringShouldIncludeId()
    {
        var id = Guid.NewGuid();
        var entity = new Employee { Id = id, FirstName = "John", LastName = "Doe" };

        entity.ToString().ShouldContain("John");
    }
}
