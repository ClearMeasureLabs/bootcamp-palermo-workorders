namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the name formatted as "LAST, I." (uppercase) for the login dropdown.
    /// Falls back to "LAST" when the first name is empty.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        var trimmed = fullName.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return string.Empty;

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        var lastName = parts[^1].ToUpperInvariant();
        var firstNamePart = string.Join(" ", parts.Take(parts.Length - 1)).Trim();

        if (string.IsNullOrEmpty(firstNamePart))
            return lastName;

        var initial = char.ToUpperInvariant(firstNamePart[0]).ToString();
        return $"{lastName}, {initial}.";
    }
}
