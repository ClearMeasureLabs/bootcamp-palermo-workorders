namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Formats the employee name as <c>LASTNAME, FIRST_INITIAL.</c> (uppercase) for the login dropdown.
    /// </summary>
    public static string FormatForLoginDropdown(string? firstName, string? lastName)
    {
        if (string.IsNullOrEmpty(lastName))
            return string.Empty;

        var lastNameUpper = lastName.ToUpperInvariant();

        // Extract first initial from firstName, falling back to first char of lastName
        char initial = !string.IsNullOrEmpty(firstName)
            ? firstName.ToUpperInvariant()[0]
            : lastNameUpper[0];

        return $"{lastNameUpper}, {initial}.";
    }
}
