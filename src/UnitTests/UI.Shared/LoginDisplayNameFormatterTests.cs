using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_FirstAndLast_ReturnsLastCommaFirstInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", "Simpson").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsUppercase()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane", "SIMPSON").ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsLastCommaFirstInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER", "SIMPSON").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_Null_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown((string?)null, null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_Empty_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty, string.Empty).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_OnlyFirstName_ReturnsUppercasedToken()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer", null).ShouldBe("HOMER");
    }

    [Test]
    public void FormatForLoginDropdown_OnlyLastName_ReturnsUppercasedToken()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null, "Simpson").ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyLastNameWithSpacedFirstName_TreatsLastTokenAsLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane", null).ShouldBe("JANE, M.");
    }

    [Test]
    public void FormatForLoginDropdown_SingleTokenFullName_ReturnsUppercasedToken()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Simpson").ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_FullName_ReturnsLastCommaFirstInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Homer Simpson").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void FormatForLoginDropdown_FullNameWithHyphenAndApostrophe_ReturnsUppercase()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane O'Brien").ShouldBe("O'BRIEN, M.");
    }
}
