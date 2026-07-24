using System.Reflection;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Reads build version and commit hash from assembly metadata.
/// </summary>
public static class VersionMetadataReader
{
    /// <summary>
    /// Returns the assembly version string used as the build version, or <see langword="null"/> when unavailable.
    /// </summary>
    public static string? ReadBuildVersion(Assembly assembly) =>
        assembly.GetName().Version?.ToString();

    /// <summary>
    /// Extracts the commit hash from an informational version containing a <c>+{sha}</c> suffix (SDK SourceRevisionId).
    /// </summary>
    public static string? ReadCommitHash(string? informationalVersion)
    {
        if (string.IsNullOrEmpty(informationalVersion))
            return null;

        var plusIndex = informationalVersion.IndexOf('+');
        if (plusIndex < 0 || plusIndex == informationalVersion.Length - 1)
            return null;

        return informationalVersion[(plusIndex + 1)..];
    }
}
