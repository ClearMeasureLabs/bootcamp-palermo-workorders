namespace ClearMeasure.Bootcamp.UI.Shared.Formatting;

/// <summary>
/// Formats byte counts as human-readable file sizes (B, KB, MB, GB).
/// </summary>
public static class FileSizeFormatter
{
    private const long Kilobyte = 1024L;
    private const long Megabyte = Kilobyte * 1024L;
    private const long Gigabyte = Megabyte * 1024L;

    /// <summary>
    /// Formats <paramref name="bytes"/> as B below 1 KB, otherwise one decimal place with KB, MB, or GB.
    /// </summary>
    public static string Format(long bytes)
    {
        if (bytes < Kilobyte)
        {
            return $"{bytes} B";
        }

        if (bytes < Megabyte)
        {
            return $"{bytes / (double)Kilobyte:0.0} KB";
        }

        if (bytes < Gigabyte)
        {
            return $"{bytes / (double)Megabyte:0.0} MB";
        }

        return $"{bytes / (double)Gigabyte:0.0} GB";
    }
}
