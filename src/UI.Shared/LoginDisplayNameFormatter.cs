namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the name formatted as "LAST, I." for the login dropdown only.
    /// Null or empty lastName → string.Empty. Null or empty firstName → "LAST" only (never uses the last name's initial).
    /// NEVER splits a full-name string on spaces — compound last names like "Lovejoy Jr" stay intact.
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
