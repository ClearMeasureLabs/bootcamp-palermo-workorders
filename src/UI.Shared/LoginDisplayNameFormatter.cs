namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the full name in uppercase so locally stored mixed-case names match mainframe all-caps names in the login drop-down.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        return fullName.ToUpperInvariant();
    }
}
