using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane", "SIMPSON").ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER", "SIMPSON").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_NullFirstName_ReturnsLastOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, "SIMPSON").ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, "SIMPSON").ShouldBe("SIMPSON");
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
    public void FormatForLoginDropdown_BothNull_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane", "O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_LastNameWithSpace_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy", "Lovejoy Jr")
            .ShouldBe("LOVEJOY JR, T.");
    }

    [Test]
    public void FormatForLoginDropdown_SingleLetterFirstName_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("A", "SMITH").ShouldBe("SMITH, A.");
    }
}
