using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class AssigneeDisplayNameFormatterTests
{
    [Test]
    public void FormatForDisplay_Gwillie_ReturnsFullName()
    {
        var willie = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "w@test.com");
        AssigneeDisplayNameFormatter.FormatForDisplay(willie).ShouldBe("Groundskeeper Willie MacDougal");
    }

    [Test]
    public void FormatForDisplay_Tlovejoy_ReturnsFullName()
    {
        var lovejoy = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "reverend@firstchurchspringfield.org");
        AssigneeDisplayNameFormatter.FormatForDisplay(lovejoy).ShouldBe("Timothy Lovejoy Jr");
    }

    [Test]
    public void FormatForDisplay_Null_ReturnsUnassigned()
    {
        AssigneeDisplayNameFormatter.FormatForDisplay(null).ShouldBe("Unassigned");
    }

    [Test]
    public void FormatForDisplay_EmptyNames_ReturnsSingleSpace()
    {
        // Documents GetFullName passthrough: empty first/last names yield a single space.
        var empty = new Employee("empty", "", "", "empty@test.com");
        AssigneeDisplayNameFormatter.FormatForDisplay(empty).ShouldBe(" ");
    }
}
