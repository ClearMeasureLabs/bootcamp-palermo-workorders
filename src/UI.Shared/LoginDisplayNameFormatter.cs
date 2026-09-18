namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the name formatted as "LAST, I." (uppercase) from separate first and last name parts.
    /// Falls back to "LAST" when FirstName is null or empty.
    /// </summary>
    public static string FormatForLoginDropdown(string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(lastName))
            return string.Empty;

        var last = lastName.ToUpperInvariant();

        if (string.IsNullOrEmpty(firstName))
            return last;

        var firstInitial = firstName[0].ToString().ToUpperInvariant();
        return $"{last}, {firstInitial}.";
    }
}
