using ClearMeasure.Bootcamp.Core.Model;

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

    [Test]
    public void ToString_named_role_returns_Name()
    {
        var role = new Role("Admin", true, false);

        Assert.That(role.ToString(), Is.EqualTo("Admin"));
    }

    [Test]
    public void ToString_default_role_returns_empty()
    {
        var role = new Role();

        Assert.That(role.ToString(), Is.EqualTo(string.Empty));
    }
}