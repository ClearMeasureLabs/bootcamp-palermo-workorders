using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_HomerSimpson_ReturnsSimpsonH()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", "SIMPSON").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_MixedCaseFirstName_ReturnsLastInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane", "SIMPSON").ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_MontgomeryBurns_ReturnsBurnsM()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Montgomery", "Burns").ShouldBe("BURNS, M.");
    }

    [Test]
    public void FormatForLoginDropdown_NedFlanders_ReturnsFlandersN()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Ned", "Flanders").ShouldBe("FLANDERS, N.");
    }

    [Test]
    public void FormatForLoginDropdown_TimothyLovejoyJr_ReturnsLovejoyJrT()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy", "Lovejoy Jr").ShouldBe("LOVEJOY JR, T.");
    }

    [Test]
    public void FormatForLoginDropdown_NullLastName_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_EmptyLastName_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", string.Empty).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_NullFirstName_FallsBackToLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, "SIMPSON").ShouldBe("SIMPSON, S.");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_FallsBackToLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, "SIMPSON").ShouldBe("SIMPSON, S.");
    }

    [Test]
    public void FormatForLoginDropdown_BothNull_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_BothEmpty_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, string.Empty).ShouldBe(string.Empty);
    }
}
