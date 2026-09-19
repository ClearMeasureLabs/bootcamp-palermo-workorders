using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary", "jane SIMPSON")
            .ShouldBe("JANE SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsLastInitialFormat()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", "SIMPSON")
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
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, "SIMPSON")
            .ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, "SIMPSON")
            .ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_BothNull_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, null)
            .ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_CompoundLastName_PreservesAllParts()
    {
        // Compound last names like "Lovejoy Jr" must give "LOVEJOY JR, T."
        LoginDisplayNameFormatter.FormatForLoginDropdown("Timothy", "Lovejoy Jr")
            .ShouldBe("LOVEJOY JR, T.");
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_LastNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary", "O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_FirstNameOnly()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane", "Smith")
            .ShouldBe("SMITH, M.");
    }
}
