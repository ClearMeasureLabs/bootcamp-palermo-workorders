namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the name formatted as "LAST, I." (LastName + comma + space + first letter of FirstName + period).
    /// Falls back to "LAST" when FirstName is null or empty.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        var lastName = parts[^1].ToUpperInvariant();

        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
            return lastName;

        var firstNameInitial = parts[0][0].ToString().ToUpperInvariant();
        return $"{lastName}, {firstNameInitial}.";
    }
}
