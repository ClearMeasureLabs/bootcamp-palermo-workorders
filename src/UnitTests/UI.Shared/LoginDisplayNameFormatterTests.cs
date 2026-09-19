using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_HomerSimpson_ReturnsSimpsonH()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", "Simpson")
            .ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_NedFlanders_ReturnsFlandersN()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Ned", "Flanders")
            .ShouldBe("FLANDERS, N.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsSameFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER", "SIMPSON")
            .ShouldBe("SIMPSON, H.");
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
    public void FormatForLoginDropdown_BothNull_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, null)
            .ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_BothEmpty_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, string.Empty)
            .ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_CompoundLastName_ReturnsFullLastNameWithInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy", "Lovejoy Jr")
            .ShouldBe("LOVEJOY JR, T.");
    }

    [Test]
    public void FormatForLoginDropdown_HyphenatedLastName_ReturnsHyphenatedLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary", "Mary-Jane")
            .ShouldBe("MARY-JANE, M.");
    }

    [Test]
    public void FormatForLoginDropdown_ApostropheLastName_ReturnsApostropheLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary", "O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_MontgomeryBurns_ReturnsBurnsM()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Montgomery", "Burns")
            .ShouldBe("BURNS, M.");
    }
}
