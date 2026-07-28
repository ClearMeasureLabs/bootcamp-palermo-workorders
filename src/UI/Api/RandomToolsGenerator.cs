using System.Globalization;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Generates random plain-text values for the tools/random API.
/// </summary>
public static class RandomToolsGenerator
{
    private const int DefaultMin = 0;
    private const int DefaultMax = 100;
    private const int DefaultStringLength = 16;
    internal const int MaxStringLength = 256;

    private const string AlphanumericCharset =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Supported <c>type</c> query values (case-insensitive).
    /// </summary>
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "number",
        "string",
        "uuid",
        "color"
    };

    /// <summary>
    /// Generates a random number as a decimal string using an exclusive upper bound.
    /// </summary>
    public static string GenerateNumber(Random random, int min = DefaultMin, int max = DefaultMax) =>
        random.Next(min, max).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Generates a random alphanumeric string of the requested length.
    /// </summary>
    public static string GenerateString(Random random, int length = DefaultStringLength)
    {
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            var index = random.Next(AlphanumericCharset.Length);
            builder.Append(AlphanumericCharset[index]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a random hex color code in <c>#RRGGBB</c> form.
    /// </summary>
    public static string GenerateColor(Random random)
    {
        var r = random.Next(256);
        var g = random.Next(256);
        var b = random.Next(256);
        return string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}");
    }

    internal static int ResolveMin(int? min) => min ?? DefaultMin;

    internal static int ResolveMax(int? max) => max ?? DefaultMax;

    internal static int ResolveStringLength(int? length) => length ?? DefaultStringLength;
}
