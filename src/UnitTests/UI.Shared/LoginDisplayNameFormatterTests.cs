using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_HomerSimpson_ReturnsSIMPSON_H_()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer Simpson").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_MaryJaneWatson_ReturnsWATSON_M_()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary Jane Watson").ShouldBe("WATSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_Burns_ReturnsBURNS_M_()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Montgomery Burns").ShouldBe("BURNS, M.");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(" Burns").ShouldBe("BURNS");
    }

    [Test]
    public void FormatForLoginDropdown_Null_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_Empty_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_WhitespaceOnly_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("  \t ").ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_ReturnsLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane O'Brien").ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_SingleWordName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Burns").ShouldBe("BURNS");
    }

    [Test]
    public void FormatForLoginDropdown_NedFlanders_ReturnsFLANDERS_N_()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Ned Flanders").ShouldBe("FLANDERS, N.");
    }
}
