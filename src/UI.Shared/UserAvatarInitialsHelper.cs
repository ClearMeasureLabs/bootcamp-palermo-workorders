using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Derives avatar initials, display name, and background color for the signed-in user header badge.
/// </summary>
public static class UserAvatarInitialsHelper
{
    /// <summary>
    /// Returns uppercase initials from the employee name, or the first two username characters when names are unavailable.
    /// </summary>
    public static string GetInitials(Employee? employee, string? username)
    {
        var firstInitial = GetNameInitial(employee?.FirstName);
        var lastInitial = GetNameInitial(employee?.LastName);

        if (!string.IsNullOrEmpty(firstInitial) && !string.IsNullOrEmpty(lastInitial))
            return firstInitial + lastInitial;

        if (!string.IsNullOrEmpty(firstInitial))
            return firstInitial;

        if (!string.IsNullOrEmpty(lastInitial))
            return lastInitial;

        if (string.IsNullOrWhiteSpace(username))
            return string.Empty;

        var trimmed = username.Trim();
        if (trimmed.Length >= 2)
            return trimmed[..2].ToUpperInvariant();

        return trimmed.ToUpperInvariant();
    }

    /// <summary>
    /// Returns a deterministic HSL background color for the given username.
    /// </summary>
    public static string GetBackgroundColor(string username)
    {
        if (string.IsNullOrEmpty(username))
            return "hsl(0, 0%, 70%)";

        var hash = GetDeterministicHash(username);
        var hue = Math.Abs(hash) % 360;
        return $"hsl({hue}, 65%, 45%)";
    }

    /// <summary>
    /// Returns the accessible display name for the avatar aria-label.
    /// </summary>
    public static string GetDisplayName(Employee? employee, string? username)
    {
        if (employee != null)
        {
            var fullName = employee.GetFullName().Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return $"Signed in as {fullName}";
        }

        if (!string.IsNullOrEmpty(username))
            return $"Signed in as {username}";

        return "Signed in";
    }

    private static string GetNameInitial(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return char.ToUpperInvariant(name.Trim()[0]).ToString();
    }

    private static int GetDeterministicHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in value)
                hash = hash * 31 + c;

            return hash;
        }
    }
}
