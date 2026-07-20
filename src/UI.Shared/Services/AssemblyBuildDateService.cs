namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Provides the assembly build date for display in the UI.
/// </summary>
public sealed class AssemblyBuildDateService
{
    private static readonly string? _buildDate = ResolveBuildDate();

    /// <summary>
    /// Gets the UTC build date formatted as <c>yyyy-MM-dd</c>, or <c>null</c> if unavailable (e.g. WebAssembly).
    /// </summary>
    public string? BuildDate => _buildDate;

    private static string? ResolveBuildDate()
    {
        var location = typeof(AssemblyBuildDateService).Assembly.Location;
        if (string.IsNullOrEmpty(location))
            return null;
        try
        {
            return new FileInfo(location).LastWriteTimeUtc.ToString("yyyy-MM-dd");
        }
        catch
        {
            return null;
        }
    }
}
