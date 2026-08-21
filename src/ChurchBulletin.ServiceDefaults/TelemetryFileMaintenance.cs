namespace ChurchBulletin.ServiceDefaults;

internal static class TelemetryFileMaintenance
{
    public static void DeleteFilesOlderThan(string directory, int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            foreach (var file in Directory.GetFiles(directory, "*.jsonl"))
            {
                if (File.GetCreationTimeUtc(file) < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public static async ValueTask DisposeWritersAsync(params TextWriter?[] writers)
    {
        foreach (var writer in writers)
        {
            if (writer is null)
            {
                continue;
            }

            await writer.DisposeAsync();
        }
    }
}
