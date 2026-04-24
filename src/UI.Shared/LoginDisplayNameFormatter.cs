namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns the full name in uppercase so locally stored mixed-case names match mainframe all-caps names in the login drop-down only.
    /// </summary>
    /// <param name="fullName">Composed full name from the domain; this helper does not persist or mutate stored data.</param>
    /// <returns>Uppercase invariant text for the login <c>&lt;select&gt;</c> labels, or empty when <paramref name="fullName"/> is null or empty.</returns>
    /// <remarks>Uses <see cref="string.ToUpperInvariant"/> for culture-stable casing (e.g. Turkish I trade-off is accepted for this display-only control).</remarks>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        return fullName.ToUpperInvariant();
    }
}
