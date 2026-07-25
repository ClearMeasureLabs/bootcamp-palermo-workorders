using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class UserAvatarInitialsHelperTests
{
    [Test]
    public void GetInitials_WithFirstAndLastName_ReturnsUppercaseInitials()
    {
        var employee = new Employee("jdoe", "Jane", "Doe", "jane@example.com");

        UserAvatarInitialsHelper.GetInitials(employee, "jdoe").ShouldBe("JD");
    }

    [Test]
    public void GetInitials_WithSingleName_ReturnsSingleInitial()
    {
        var employee = new Employee("homer", "Homer", "", "homer@example.com");

        UserAvatarInitialsHelper.GetInitials(employee, "homer").ShouldBe("H");
    }

    [Test]
    public void GetInitials_WithNoEmployee_UseUsernameFirstTwoChars()
    {
        UserAvatarInitialsHelper.GetInitials(null, "hsimpson").ShouldBe("HS");
    }

    [Test]
    public void GetInitials_WithNullUsernameAndNoEmployee_ReturnsEmptyString()
    {
        UserAvatarInitialsHelper.GetInitials(null, null).ShouldBe(string.Empty);
    }

    [Test]
    public void GetBackgroundColor_SameUsername_ReturnsDeterministicColor()
    {
        var first = UserAvatarInitialsHelper.GetBackgroundColor("hsimpson");
        var second = UserAvatarInitialsHelper.GetBackgroundColor("hsimpson");

        first.ShouldBe(second);
        first.ShouldStartWith("hsl(");
    }

    [Test]
    public void GetBackgroundColor_DifferentUsernames_ReturnsDifferentColors()
    {
        var homer = UserAvatarInitialsHelper.GetBackgroundColor("hsimpson");
        var marge = UserAvatarInitialsHelper.GetBackgroundColor("mbouvier");

        homer.ShouldNotBe(marge);
    }

    [Test]
    public void GetDisplayName_WithEmployee_ReturnsFullName()
    {
        var employee = new Employee("jdoe", "Jane", "Doe", "jane@example.com");

        UserAvatarInitialsHelper.GetDisplayName(employee, "jdoe").ShouldBe("Signed in as Jane Doe");
    }

    [Test]
    public void GetDisplayName_WithoutEmployee_ReturnsUsernameOnly()
    {
        UserAvatarInitialsHelper.GetDisplayName(null, "hsimpson").ShouldBe("Signed in as hsimpson");
    }
}
