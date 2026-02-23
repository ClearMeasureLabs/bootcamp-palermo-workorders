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
        var employee1 = new Employee { Id = id };
        var employee2 = new Employee { Id = id };

        employee1.Equals(employee2).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeEqualWhenDifferentIds()
    {
        var employee1 = new Employee { Id = Guid.NewGuid() };
        var employee2 = new Employee { Id = Guid.NewGuid() };

        employee1.Equals(employee2).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqualWhenBothIdsAreEmpty()
    {
        var employee1 = new Employee();
        var employee2 = new Employee();

        employee1.Equals(employee2).ShouldBeFalse();
    }

    [Test]
    public void ShouldBeEqualWhenSameReference()
    {
        var employee = new Employee { Id = Guid.NewGuid() };

        employee.Equals(employee).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeEqualToNull()
    {
        var employee = new Employee { Id = Guid.NewGuid() };

        employee.Equals((Employee?)null).ShouldBeFalse();
        employee.Equals((object?)null).ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeEqualToDifferentType()
    {
        var employee = new Employee { Id = Guid.NewGuid() };

        employee.Equals("not an employee").ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnConsistentHashCode()
    {
        var id = Guid.NewGuid();
        var employee1 = new Employee { Id = id };
        var employee2 = new Employee { Id = id };

        employee1.GetHashCode().ShouldBe(employee2.GetHashCode());
    }

    [Test]
    public void ShouldIncludeIdInToString()
    {
        var id = Guid.NewGuid();
        var employee = new Employee { Id = id };

        employee.ToString().ShouldNotBeNull();
    }

    [Test]
    public void ShouldSupportEqualityOperator()
    {
        var id = Guid.NewGuid();
        var employee1 = new Employee { Id = id };
        var employee2 = new Employee { Id = id };

        (employee1 == employee2).ShouldBeTrue();
        (employee1 != employee2).ShouldBeFalse();
    }

    [Test]
    public void ShouldSupportInequalityOperator()
    {
        var employee1 = new Employee { Id = Guid.NewGuid() };
        var employee2 = new Employee { Id = Guid.NewGuid() };

        (employee1 != employee2).ShouldBeTrue();
        (employee1 == employee2).ShouldBeFalse();
    }

    [Test]
    public void ShouldHandleNullWithOperators()
    {
        var employee = new Employee { Id = Guid.NewGuid() };

        (employee == null).ShouldBeFalse();
        (null == employee).ShouldBeFalse();
        (employee != null).ShouldBeTrue();

        Employee? nullEmployee = null;
        (nullEmployee == null).ShouldBeTrue();
    }
}
