namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display-only formatting for the login page "Select Church Member" drop-down.
/// Does not mutate stored <c>Employee</c> names or apply elsewhere in the UI.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the full name in ALL CAPS (<see cref="string.ToUpperInvariant"/>)
    /// so locally stored mixed-case names match mainframe all-caps labels.
    /// Null or empty input yields <see cref="string.Empty"/>. Username option values stay unchanged.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        return fullName.ToUpperInvariant();
    }
}
