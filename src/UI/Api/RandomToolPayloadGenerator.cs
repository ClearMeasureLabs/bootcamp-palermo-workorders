using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Default <see cref="IRandomToolPayloadGenerator"/> using <see cref="Random"/> and <see cref="RandomNumberGenerator"/>.
/// </summary>
public sealed class RandomToolPayloadGenerator : IRandomToolPayloadGenerator
{
    private static ReadOnlySpan<char> AlphanumericChars =>
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <inheritdoc />
    public int NextInt32() => Random.Shared.Next();

    /// <inheritdoc />
    public string NextAlphanumericString(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

        var chars = AlphanumericChars;
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = chars[Random.Shared.Next(chars.Length)];

        return new string(buffer);
    }

    /// <inheritdoc />
    public Guid NextGuid() => Guid.NewGuid();

    /// <inheritdoc />
    public string NextCssHexColor()
    {
        Span<byte> rgb = stackalloc byte[3];
        RandomNumberGenerator.Fill(rgb);
        var sb = new StringBuilder(7);
        sb.Append('#');
        sb.Append(rgb[0].ToString("x2", null));
        sb.Append(rgb[1].ToString("x2", null));
        sb.Append(rgb[2].ToString("x2", null));
        return sb.ToString();
    }
}
