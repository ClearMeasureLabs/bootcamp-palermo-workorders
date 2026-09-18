namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Formats a full name for the login dropdown as "LAST, I." (uppercase).
    /// When FirstName is empty, returns "LAST" only.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var parts = fullName.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        var lastName = parts[^1].ToUpperInvariant();

        // Build the first name initial from all leading parts (FirstName, middle names, etc.)
        var firstNameParts = parts.Take(parts.Length - 1).ToArray();
        if (firstNameParts.Length == 0)
        {
            // No first name — return LAST only
            return lastName;
        }

        var initial = firstNameParts[0][0].ToString().ToUpperInvariant();
        return $"{lastName}, {initial}.";
    }
}
