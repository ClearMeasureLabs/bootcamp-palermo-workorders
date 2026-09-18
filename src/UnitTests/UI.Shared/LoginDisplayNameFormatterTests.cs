using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_TwoPartName_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer Simpson")
            .ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane SIMPSON")
            .ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER SIMPSON")
            .ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_ThreePartName_ReturnsLastInitialFormat()
    {
        // "Timothy Lovejoy Jr" → last word "Jr" is treated as last name
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy Lovejoy Jr")
            .ShouldBe("JR, T.");
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
    public void FormatForLoginDropdown_HyphenAndApostrophe_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_LastNameOnly_ReturnsLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("SIMPSON")
            .ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(" SIMPSON")
            .ShouldBe("SIMPSON");
    }
}
