namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the display name for the login dropdown: LAST, I. format.
    /// Null or empty lastName → string.Empty. Null or empty firstName → LAST only.
    /// NEVER splits a full-name string on spaces.
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
