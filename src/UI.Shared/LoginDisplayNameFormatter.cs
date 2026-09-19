namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the display name for the login dropdown: "LAST, F." where LAST is the
    /// uppercase last name and F. is the uppercase first initial.  Rules:
    ///   • null or empty lastName → string.Empty
    ///   • null or empty firstName → "LAST" only (no trailing comma)
    ///   • NEVER split a full-name string on spaces — compound last names stay intact.
    /// </summary>
    public static string FormatForLoginDropdown(string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(lastName))
            return string.Empty;

        var last = lastName.ToUpperInvariant();

        if (string.IsNullOrEmpty(firstName))
            return last;

        var firstInitial = char.ToUpperInvariant(firstName[0]).ToString();
        return $"{last}, {firstInitial}.";
    }
}
