namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    /// <summary>
    /// Returns "LAST, I." in uppercase so locally stored mixed-case names match mainframe all-caps names in the login drop-down.
    /// </summary>
    public static string FormatForLoginDropdown(string? firstName, string? lastName)
    {
        var first = firstName?.Trim();
        var last = lastName?.Trim();

        if (string.IsNullOrEmpty(last))
        {
            if (string.IsNullOrEmpty(first))
                return string.Empty;

            var tokens = first.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1)
                return tokens[0].ToUpperInvariant();

            last = tokens[^1];
            first = tokens[0];
        }

        if (string.IsNullOrEmpty(first))
            return last.ToUpperInvariant();

        var initial = first[0].ToString().ToUpperInvariant();
        return $"{last.ToUpperInvariant()}, {initial}.";
    }

    /// <summary>
    /// Returns "LAST, I." in uppercase from a full name; the last whitespace token is treated as the last name and the first token as the first name.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var tokens = fullName.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 1)
            return tokens[0].ToUpperInvariant();

        return FormatForLoginDropdown(tokens[0], tokens[^1]);
    }
}
