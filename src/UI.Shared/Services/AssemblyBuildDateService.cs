using System.Reflection;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Provides the assembly build date embedded at compile time, readable in both server and WebAssembly contexts.
/// </summary>
public sealed class AssemblyBuildDateService
{
    private static readonly string? _buildDate =
        typeof(AssemblyBuildDateService).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value;

    /// <summary>
    /// Gets the UTC build date formatted as <c>yyyy-MM-dd</c>, or <c>null</c> if not embedded.
    /// </summary>
    public string? BuildDate => _buildDate;
}
