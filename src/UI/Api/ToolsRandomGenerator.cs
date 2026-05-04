using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds pseudo-random payloads for <see cref="Controllers.ToolsRandomController"/>.
/// </summary>
internal static class ToolsRandomGenerator
{
    internal const int DefaultStringOrByteLength = 16;
    internal const int MaxStringOrByteLength = 4096;
    internal const int MaxCharsetLength = 512;
    internal const int DefaultIntMinInclusive = 0;
    internal const int DefaultIntMaxExclusive = 101;
    internal const long DefaultLongMinInclusive = 0L;
    internal const long DefaultLongMaxExclusive = 101L;

    internal static readonly string DefaultCharset =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    internal static int NextInt(Random random, int minInclusive, int maxExclusive) =>
        random.Next(minInclusive, maxExclusive);

    internal static long NextLong(Random random, long minInclusive, long maxExclusive) =>
        random.NextInt64(minInclusive, maxExclusive);

    internal static Guid NextGuid(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    internal static string NextString(Random random, int length, ReadOnlySpan<char> charset)
    {
        if (length == 0)
            return string.Empty;

        var buffer = length <= 1024 ? stackalloc char[length] : new char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = charset[random.Next(charset.Length)];
        return buffer.Length == 0 ? string.Empty : new string(buffer);
    }

    internal static string NextBytesEncoded(Random random, int length, BytesEncoding encoding)
    {
        if (length == 0)
            return encoding == BytesEncoding.Hex ? string.Empty : Convert.ToBase64String(ReadOnlySpan<byte>.Empty);

        var bytes = length <= 4096 ? stackalloc byte[length] : new byte[length];
        random.NextBytes(bytes);
        return encoding == BytesEncoding.Hex ? Convert.ToHexString(bytes) : Convert.ToBase64String(bytes);
    }

    internal static bool TryParseKind(string? raw, out RandomKind kind)
    {
        kind = RandomKind.Int;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return Enum.TryParse(raw, ignoreCase: true, out kind);
    }

    internal static bool TryParseBytesEncoding(string? raw, out BytesEncoding encoding)
    {
        encoding = BytesEncoding.Base64;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return Enum.TryParse(raw, ignoreCase: true, out encoding);
    }

    internal static bool TryParseBoundedInt(
        string? raw,
        int min,
        int max,
        string parameterName,
        out int value,
        out string? error)
    {
        value = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = DefaultStringOrByteLength;
            return true;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = $"{parameterName} must be a 32-bit integer.";
            return false;
        }

        if (value < min || value > max)
        {
            error = $"{parameterName} must be between {min} and {max}.";
            return false;
        }

        return true;
    }

    internal static bool TryParseLongBounds(
        string? minRaw,
        string? maxRaw,
        out long minInclusive,
        out long maxExclusive,
        out string? error)
    {
        error = null;
        minInclusive = DefaultLongMinInclusive;
        maxExclusive = DefaultLongMaxExclusive;

        if (!string.IsNullOrWhiteSpace(minRaw))
        {
            if (!long.TryParse(minRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out minInclusive))
            {
                error = "min must be a 64-bit integer.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(maxRaw))
        {
            if (!long.TryParse(maxRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxExclusive))
            {
                error = "max must be a 64-bit integer.";
                return false;
            }
        }

        if (minInclusive >= maxExclusive)
        {
            error = "min must be less than max (max is exclusive).";
            return false;
        }

        return true;
    }

    internal static bool TryParseIntBounds(
        string? minRaw,
        string? maxRaw,
        out int minInclusive,
        out int maxExclusive,
        out string? error)
    {
        error = null;
        minInclusive = DefaultIntMinInclusive;
        maxExclusive = DefaultIntMaxExclusive;

        if (!string.IsNullOrWhiteSpace(minRaw))
        {
            if (!int.TryParse(minRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out minInclusive))
            {
                error = "min must be a 32-bit integer.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(maxRaw))
        {
            if (!int.TryParse(maxRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxExclusive))
            {
                error = "max must be a 32-bit integer.";
                return false;
            }
        }

        if (minInclusive >= maxExclusive)
        {
            error = "min must be less than max (max is exclusive).";
            return false;
        }

        return true;
    }

    internal static bool TryValidateCharset(string? charset, out string resolved, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(charset))
        {
            resolved = DefaultCharset;
            return true;
        }

        if (charset.Length > MaxCharsetLength)
        {
            resolved = string.Empty;
            error = $"charset must be at most {MaxCharsetLength} characters.";
            return false;
        }

        resolved = charset;
        return true;
    }
}

internal enum RandomKind
{
    Int,
    Long,
    Guid,
    String,
    Bytes
}

internal enum BytesEncoding
{
    Base64,
    Hex
}
