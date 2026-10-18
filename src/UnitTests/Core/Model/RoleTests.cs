using ClearMeasure.Bootcamp.Core.Model;

using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class RoleTests
{
    [Test]
    public void Role_defaults_properly()
    {
        var role = new Role();

        Assert.That(role.Name, Is.Null);
        Assert.That(role.Id, Is.EqualTo(Guid.Empty));
        Assert.That(role.CanCreateWorkOrder, Is.False);
        Assert.That(role.CanFulfillWorkOrder, Is.False);

        var role2 = new Role("roleName", true, true);

        Assert.That(role2.Name, Is.EqualTo("roleName"));
        Assert.That(role2.CanCreateWorkOrder, Is.True);
        Assert.That(role2.CanFulfillWorkOrder, Is.True);
    }

    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    public void IsCreatorOnly_ShouldReturnExpected_ForPermissionCombinations(
        bool canCreate, bool canFulfill, bool expected)
    {
        var role = new Role("r", canCreate, canFulfill);

        role.IsCreatorOnly().ShouldBe(expected);
    }
}