using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Compares strings so embedded numbers order by numeric value (e.g. "Room 9" before "Room 10").
/// </summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    private static readonly Regex Digits = new(@"\d+", RegexOptions.Compiled);

    /// <summary>Ordinal ignore-case natural string comparer.</summary>
    public static NaturalStringComparer OrdinalIgnoreCase { get; } = new();

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    private NaturalStringComparer()
    {
    }

    /// <inheritdoc />
    public int Compare(string? x, string? y) =>
        string.Compare(Normalize(x), Normalize(y), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value)
    {
        if (value is null) return string.Empty;
        return Digits.Replace(value, static m => m.Value.PadLeft(20, '0'));
    }
}
