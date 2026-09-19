namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Formats first and last name for the login dropdown: "LAST, I." (e.g. "SIMPSON, H.").
    /// Null or empty last name returns string.Empty; null or empty first name returns "LAST" only.
    /// Never splits a full-name string on spaces — compound last names are preserved.
    /// </summary>
    public static string FormatForLoginDropdown(string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(lastName))
            return string.Empty;

        var last = lastName.ToUpperInvariant();

        if (string.IsNullOrEmpty(firstName))
            return last;

        var firstInitial = firstName.ToUpperInvariant()[0];
        return last + ", " + firstInitial + ".";
    }
}
