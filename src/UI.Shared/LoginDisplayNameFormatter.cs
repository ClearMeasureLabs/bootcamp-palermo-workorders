namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// Returns LAST, I. (uppercase last name, comma, space, uppercase first initial, period).
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Formats the name as LAST, I. for the login dropdown.
    /// Falls back to LAST only when FirstName is null or empty.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        var lastName = parts[^1].ToUpperInvariant();

        if (parts.Length == 1)
            return lastName;

        var firstNameInitial = parts[0][0].ToString().ToUpperInvariant();
        return $"{lastName}, {firstNameInitial}.";
    }
}
