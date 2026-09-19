using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsLastInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary Jane", "SIMPSON")
            .ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_HomerSimpson_ReturnsLastInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", "Simpson")
            .ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_NedFlanders_ReturnsLastInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Ned", "Flanders")
            .ShouldBe("FLANDERS, N.");
    }

    [Test]
    public void FormatForLoginDropdown_NullLastName_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", null)
            .ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_EmptyLastName_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", string.Empty)
            .ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_NullFirstName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, "Simpson")
            .ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, "Simpson")
            .ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_CompoundLastName_PreservesFullLast()
    {
        // Compound last names must NOT be split — Lovejoy Jr stays intact.
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy", "Lovejoy Jr")
            .ShouldBe("LOVEJOY JR, T.");
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_ReturnsLastInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane", "O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }
}
